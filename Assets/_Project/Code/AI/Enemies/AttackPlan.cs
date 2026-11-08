using System;
using Eclipse.AI.Telegraphs;
using Eclipse.Core.Damage;
using Eclipse.Core.Random;
using Eclipse.Core.Tags;
using Eclipse.Data.Enemies;
using Eclipse.Gameplay.Simulation;

namespace Eclipse.AI.Enemies
{
    /// <summary>
    /// Runtime view of one <see cref="AttackDefinition"/> for one spawned enemy: effective wind-up (difficulty reduction and
    /// floor via <see cref="WindupMath"/>), seconds instead of frames, absolute damage. Built once at spawn; the combo
    /// (<c>chain_next</c>) is a linked list of plans.
    /// </summary>
    public sealed class AttackPlan
    {
        private const int MaxChainLength = 8;

        public AttackDefinition Definition { get; private set; }
        /// <summary>Stable per attack, carried by projectiles/areas so the landing handler can find the plan.</summary>
        public int HitGroup { get; private set; }
        public AttackPlan Next { get; private set; }

        public float MinRange { get; private set; }
        public float MaxRange { get; private set; }
        public float CooldownSeconds { get; private set; }
        public float Weight { get; private set; }
        public bool OnDeath { get; private set; }
        public float TriggerChance { get; private set; }

        /// <summary>Wind-up after the difficulty reduction, never below the floor of its class (06 section 3.10).</summary>
        public float WindupSeconds { get; private set; }
        public float ActiveSeconds { get; private set; }
        public float RecoverSeconds { get; private set; }
        public bool Interruptible { get; private set; }

        public float Damage { get; private set; }
        public float StaggerValue { get; private set; }
        public DamageType DamageType { get; private set; }
        public HitFlags Flags { get; private set; }
        public TagMask Tags { get; private set; }

        public AttackDelivery Delivery { get; private set; }
        public bool Ranged { get; private set; }
        public TelegraphVisual Visual { get; private set; }
        public TelegraphShape Shape { get; private set; }
        public float Radius { get; private set; }
        public float ArcDegrees { get; private set; }
        public float Length { get; private set; }
        public float Width { get; private set; }
        public float InnerRadius { get; private set; }
        public float ProjectileSpeed { get; private set; }
        public string ZoneStatus { get; private set; }
        public float ZoneDurationSeconds { get; private set; }

        /// <summary>
        /// Melee circle whose radius is smaller than the reach (Husk lunge: 1 m circle, 2 m range) is centered this far ahead
        /// of the enemy so the strike covers the reach; derived from data (<c>range - radius</c>), 0 for the others.
        /// </summary>
        public float ForwardOffset { get; private set; }

        public bool IsLobbed => Delivery == AttackDelivery.Lobbed;
        public bool IsStraightProjectile => Delivery == AttackDelivery.Projectile;
        /// <summary>Fired from a distance: needs a clear wall line of sight and is never started off-screen (06 section 3.10).</summary>
        public bool IsProjectileLike => Delivery == AttackDelivery.Projectile || Delivery == AttackDelivery.Lobbed;

        /// <summary>Seconds the combo takes from the first wind-up to the end of the last active window (the brain's Attack state).</summary>
        public float ChainAttackSeconds
        {
            get
            {
                float total = 0f;
                int guard = 0;
                for (AttackPlan p = this; p != null && guard < MaxChainLength; p = p.Next, guard++)
                {
                    total += p.WindupSeconds + p.ActiveSeconds;
                    if (p.Next != null) total += p.RecoverSeconds;
                }
                return total;
            }
        }

        /// <summary>Recovery after the last strike of the combo (the brain's Recover state).</summary>
        public float ChainRecoverSeconds
        {
            get
            {
                AttackPlan p = this;
                int guard = 0;
                while (p.Next != null && guard++ < MaxChainLength) p = p.Next;
                return p.RecoverSeconds;
            }
        }

        public static AttackPlan Build(AttackDefinition def, in EnemyStats stats, DamageType damageType,
            float windupReduction, WindupClass windupClass)
        {
            return Build(def, in stats, damageType, windupReduction, windupClass, 0);
        }

        private static AttackPlan Build(AttackDefinition def, in EnemyStats stats, DamageType damageType,
            float windupReduction, WindupClass windupClass, int depth)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            float min = 0f, max = def.RangeMeters;
            AttackCondition.Parse(def.Condition, ref min, ref max);

            bool sniper = WindupMath.IsSniper(def.TelegraphShape, def.Delivery);
            bool annihilation = def.TelegraphType == TelegraphType.Annihilation;
            bool ranged = def.TokenKind == TokenKind.Ranged;
            TagMask tags = new TagMask(Tag.Attack);
            bool melee = def.Delivery == AttackDelivery.Melee || def.Delivery == AttackDelivery.OnDeath;
            tags = tags.With(melee ? Tag.Melee : Tag.Ranged);
            if (def.Delivery == AttackDelivery.Projectile) tags = tags.With(Tag.Projectile);
            if (def.TelegraphShape == TelegraphShape.Circle || def.TelegraphShape == TelegraphShape.Ring) tags = tags.With(Tag.Area);

            var plan = new AttackPlan
            {
                Definition = def,
                HitGroup = HitGroupOf(def),
                MinRange = min,
                MaxRange = max,
                CooldownSeconds = def.CooldownSeconds,
                Weight = def.Weight,
                OnDeath = def.Trigger == AttackTrigger.OnDeath,
                TriggerChance = def.TriggerChance,
                WindupSeconds = WindupMath.Effective(def.WindupSeconds, windupReduction, windupClass, annihilation, sniper),
                ActiveSeconds = def.ActiveFrames / (float)SimulationFrame.TicksPerSecond,
                RecoverSeconds = def.RecoveryFrames / (float)SimulationFrame.TicksPerSecond,
                Interruptible = def.Interruptible,
                Damage = EnemyStatsBuilder.AttackDamage(in stats, def),
                StaggerValue = def.Stagger,
                DamageType = damageType,
                Flags = def.HitFlags,
                Tags = tags,
                Delivery = def.Delivery,
                Ranged = ranged,
                Visual = sniper ? TelegraphVisual.SniperLine : TelegraphVisuals.From(def.TelegraphType),
                Shape = def.TelegraphShape,
                Radius = def.RadiusMeters,
                ArcDegrees = def.ArcDegrees,
                Length = def.LengthMeters,
                Width = def.WidthMeters,
                InnerRadius = def.InnerRadiusMeters,
                ProjectileSpeed = def.ProjectileSpeed,
                ZoneStatus = def.ZoneStatus,
                ZoneDurationSeconds = def.ZoneDurationSeconds,
            };
            if (def.Delivery == AttackDelivery.Melee && def.TelegraphShape == TelegraphShape.Circle && def.RadiusMeters < def.RangeMeters)
                plan.ForwardOffset = def.RangeMeters - def.RadiusMeters;
            if (def.ChainNext != null && depth + 1 < MaxChainLength)
                plan.Next = Build(def.ChainNext, in stats, damageType, windupReduction, windupClass, depth + 1);
            return plan;
        }

        /// <summary>Stable hit group of an attack: FNV-1a of its StableId (never <c>string.GetHashCode</c>), kept positive.</summary>
        public static int HitGroupOf(AttackDefinition def) =>
            (int)(RngStreams.Fnv1a64(def.StableIdString ?? string.Empty) & 0x3FFFFFFFUL) | 0x40000000;
    }
}

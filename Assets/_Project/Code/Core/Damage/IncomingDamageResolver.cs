using System;
using Eclipse.Core.Random;
using Eclipse.Core.Tags;

namespace Eclipse.Core.Damage
{
    /// <summary>Why a hit did no damage at all (06 3.3).</summary>
    public enum NegationReason : byte
    {
        None = 0,
        /// <summary>Dodge i-frames, Intangible or revive window (step 0).</summary>
        IFrames = 1,
        /// <summary>Active Parry window (step 0).</summary>
        Parry = 2,
        /// <summary>Dodge Chance roll succeeded (step 2).</summary>
        Dodge = 3,
    }

    /// <summary>
    /// Player defenses at the moment of the hit: final stat values (already capped by <c>StatBlock</c>/<c>IStatRules</c>)
    /// plus transient state. Fractions (0.25 = 25%). Reusable.
    /// </summary>
    public sealed class IncomingDefense
    {
        public float Armor;
        public float PhysicalResistance;
        public float ElementalResistance;
        public float VoidResistance;
        public float DodgeChance;
        public float BlockChance;
        /// <summary>Damage removed from a blocked hit (01 3.4.1: 0.40 base; named items may change it).</summary>
        public float BlockAmount;
        public float DamageReduction;
        /// <summary>Current Shield/Barrier points that absorb before Health.</summary>
        public float ShieldPoints;
        /// <summary>Dodge i-frames, Intangible or revive window are active (06 3.14).</summary>
        public bool HasIFrames;
        /// <summary>A Parry window (0.2 s, R20) is open; set by the caller from frame data.</summary>
        public bool ParryActive;
    }

    /// <summary>Per-hit context supplied by the caller (06 3.3). Pure data.</summary>
    public readonly struct IncomingContext
    {
        public IncomingContext(float attackerLevel, float difficultyResPenalty, bool isTelegraphed, bool blockArcValid)
        {
            AttackerLevel = attackerLevel;
            DifficultyResPenalty = difficultyResPenalty;
            IsTelegraphed = isTelegraphed;
            BlockArcValid = blockArcValid;
        }

        /// <summary>eLvl, Monster Level of the attacker (K of Armor).</summary>
        public float AttackerLevel { get; }
        /// <summary>Resistance penalty of the difficulty as a fraction (Eclipse = 0.25; 01 3.4.1).</summary>
        public float DifficultyResPenalty { get; }
        /// <summary>The attack had a ground/visual telegraph; Dodge Chance only applies when false (01 3.4.1).</summary>
        public bool IsTelegraphed { get; }
        /// <summary>Hit comes from the 180-degree frontal arc; computed by the caller (geometry is not Core).</summary>
        public bool BlockArcValid { get; }
    }

    /// <summary>Outcome of one incoming resolution, with the damage after each stage.</summary>
    public readonly struct IncomingResult
    {
        public IncomingResult(NegationReason negation, float incoming, float afterMitigation, bool blocked, float afterBlock,
            float afterDamageReduction, float shieldAbsorbed, float healthDamage)
        {
            Negation = negation;
            Incoming = incoming;
            AfterMitigation = afterMitigation;
            Blocked = blocked;
            AfterBlock = afterBlock;
            AfterDamageReduction = afterDamageReduction;
            ShieldAbsorbed = shieldAbsorbed;
            HealthDamage = healthDamage;
        }

        public NegationReason Negation { get; }
        public bool IsNegated => Negation != NegationReason.None;
        /// <summary>Total damage entering the pipeline (after difficulty scaling, before defenses).</summary>
        public float Incoming { get; }
        public float AfterMitigation { get; }
        public bool Blocked { get; }
        public float AfterBlock { get; }
        public float AfterDamageReduction { get; }
        /// <summary>Part absorbed by the Shield; the caller subtracts it from the shield.</summary>
        public float ShieldAbsorbed { get; }
        /// <summary>Part that reduces Health.</summary>
        public float HealthDamage { get; }
    }

    /// <summary>
    /// Incoming damage pipeline of 06 3.3 / 01 3.4.1: i-frames/Parry -> Dodge -> mitigation -> Block ->
    /// Damage Reduction -> Shield -> Health. Stateless apart from caps; allocation-free.
    /// Rolls use the <c>Combat</c> stream: Dodge first, then Block, each only when applicable.
    /// </summary>
    public sealed class IncomingDamageResolver
    {
        private readonly MitigationRules _rules;

        public IncomingDamageResolver(MitigationRules rules)
        {
            _rules = rules;
        }

        public IncomingResult Resolve(DamageInstance hit, IncomingDefense defense, in IncomingContext ctx, RngStreams rng)
        {
            if (hit == null) throw new ArgumentNullException(nameof(hit));
            if (defense == null) throw new ArgumentNullException(nameof(defense));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            HitFlags flags = hit.Flags;
            bool annihilation = (flags & HitFlags.Annihilation) != 0;
            bool unavoidable = (flags & HitFlags.Unavoidable) != 0;
            bool unblockable = (flags & HitFlags.Unblockable) != 0;
            bool isTick = hit.Kind == HitKind.Tick;
            float incoming = hit.TotalDamage;

            // Step 0: i-frames negate everything but Annihilation (incl. Unavoidable); Parry also fails vs
            // Unblockable/Annihilation and only parries Hits, not Ticks.
            if (!annihilation)
            {
                if (defense.HasIFrames)
                    return Negated(NegationReason.IFrames, incoming);
                if (defense.ParryActive && !unblockable && !isTick)
                    return Negated(NegationReason.Parry, incoming);
            }

            // Step 2: Dodge Chance - not Annihilation, Unavoidable, telegraphed or Tick.
            if (!annihilation && !unavoidable && !ctx.IsTelegraphed && !isTick)
            {
                float dodge = Clamp(defense.DodgeChance, 0f, _rules.DodgeCap);
                if (rng.Get(RngStream.Combat).Chance(dodge))
                    return Negated(NegationReason.Dodge, incoming);
            }

            // Step 3: mitigation per damage component (Physical two-layer / Elemental / Void).
            float mitigated = 0f;
            for (int i = 0; i < hit.ComponentCount; i++)
            {
                DamageComponent c = hit.GetComponent(i);
                float mit = MitigationMath.Mitigation(c.Type, defense, ctx.AttackerLevel, ctx.DifficultyResPenalty, _rules);
                mitigated += c.Value * (1f - mit);
            }

            // Step 4: Block - not Annihilation, Unavoidable, Unblockable or Tick; needs the frontal arc.
            bool blocked = false;
            float afterBlock = mitigated;
            if (!annihilation && !unavoidable && !unblockable && !isTick && ctx.BlockArcValid)
            {
                float block = Clamp(defense.BlockChance, 0f, _rules.BlockCap);
                if (rng.Get(RngStream.Combat).Chance(block))
                {
                    blocked = true;
                    afterBlock = mitigated * (1f - Clamp(defense.BlockAmount, 0f, 1f));
                }
            }

            // Step 5: Damage Reduction (cap).
            float afterDr = afterBlock * (1f - Clamp(defense.DamageReduction, 0f, _rules.DamageReductionCap));

            // Step 6-7: Shield absorbs first, remainder hits Health.
            float shield = defense.ShieldPoints > 0f ? defense.ShieldPoints : 0f;
            float absorbed = afterDr < shield ? afterDr : shield;
            float health = afterDr - absorbed;

            return new IncomingResult(NegationReason.None, incoming, mitigated, blocked, afterBlock, afterDr, absorbed, health);
        }

        private static IncomingResult Negated(NegationReason reason, float incoming) =>
            new IncomingResult(reason, incoming, 0f, false, 0f, 0f, 0f, 0f);

        private static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
    }
}

using System;
using Eclipse.Core.Damage;
using Eclipse.Core.Random;
using Eclipse.Core.Stats;
using Eclipse.Core.Tags;
using Eclipse.Gameplay.Simulation;
using UnityEngine;
using InvState = Eclipse.Gameplay.Player.InvulnerabilityState;
using InvHitResult = Eclipse.Gameplay.Player.HitResult;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Combat entity as plain C# (a MonoBehaviour shell only mirrors Position/Facing and calls <see cref="Tick"/>).
    /// Owns StatBlock, health, shield, grace, i-frames and death. Player-team entities run the full incoming pipeline of
    /// 06 3.3; other teams receive hits already mitigated by the offensive resolver (01 3.4.2, CLAUDE.md 4), so only
    /// Shield and Health apply.
    /// </summary>
    public sealed class CombatEntity : ICombatEntity, IDamageReceiver, IFixedTickable
    {
        private readonly CombatEntityConfig _cfg;
        private readonly IncomingDamagePipeline _pipeline;
        private readonly DamageApplier _applier;
        private readonly GracePeriod _grace;

        public CombatEntity(EntityId id, Team team, StatBlock stats, CombatEntityConfig cfg, CombatEventStream events, RngStreams rng)
        {
            Id = id;
            Team = team;
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
            Events = events;
            Health = new HealthComponent(stats.Evaluate(StatId.MaximumHealth));
            Shield = new ShieldComponent();
            Recap = new DeathRecap();
            Death = new DeathHandler(cfg, events, Recap);
            Invulnerability = new InvState();
            Clock = new EntityClock();
            _grace = new GracePeriod(cfg);
            _pipeline = new IncomingDamagePipeline(cfg, rng);
            _applier = new DamageApplier(Health, Shield, Death, events, cfg);
            IncomingAttackerLevel = 1f;
        }

        public EntityId Id { get; }
        public Team Team { get; }
        public bool IsAlive => !Death.IsDead;
        public Vector3 Position { get; set; }
        public EntityClock Clock { get; }
        public StatBlock Stats { get; }

        public HealthComponent Health { get; }
        public ShieldComponent Shield { get; }
        public DeathRecap Recap { get; }
        public DeathHandler Death { get; }
        public InvState Invulnerability { get; }
        public CombatEventStream Events { get; }

        /// <summary>Forward direction used for the 180-degree Block arc (06 3.3 step 4).</summary>
        public Vector3 Facing { get; set; } = Vector3.forward;
        public bool IsElite { get; set; }
        /// <summary>Global simulation frame; updated by <see cref="Tick"/> (tests may set it directly).</summary>
        public int CurrentFrame { get; set; }
        /// <summary>Parry window is open while <c>CurrentFrame &lt;= ParryActiveUntilFrame</c>; written by the state system (T7, 06 3.8.3).</summary>
        public int ParryActiveUntilFrame { get; set; } = -1;
        /// <summary>eLvl used by <see cref="IDamageReceiver.ReceiveHit"/> when no <see cref="IncomingHitInfo"/> is given (set from room scaling).</summary>
        public float IncomingAttackerLevel { get; set; }
        /// <summary>Active status bitmask recorded in the Death Recap (filled by the status system, M11).</summary>
        public ulong ActiveStatusMask { get; set; }

        private bool UsesPlayerPipeline => Team == Team.Player;

        public void Tick(in SimulationFrame frame) => CurrentFrame = frame.Index;

        /// <summary>Recomputes max health from Stats, keeping the current fraction.</summary>
        public void SyncMaxHealth() => Health.SetMax(Stats.Evaluate(StatId.MaximumHealth), true);

        /// <summary>Frozen-contract entry point: assumes a telegraphed hit at <see cref="IncomingAttackerLevel"/>.</summary>
        public DamageOutcome ReceiveHit(DamageInstance hit, Vector3 hitDirection) =>
            ReceiveHit(hit, hitDirection, new IncomingHitInfo(IncomingAttackerLevel, true));

        public DamageOutcome ReceiveHit(DamageInstance hit, Vector3 hitDirection, in IncomingHitInfo info)
        {
            if (hit == null) throw new ArgumentNullException(nameof(hit));
            int frame = CurrentFrame;
            var self = Id;
            if (!IsAlive) return new DamageOutcome(HitResult.Invulnerable, 0f, 0f, 0f, false);

            if (!UsesPlayerPipeline)
            {
                float total = hit.TotalDamage;
                float shield = Shield.Points;
                float absorbed = total < shield ? total : shield;
                var pre = new IncomingResult(NegationReason.None, total, total, false, total, total, absorbed, total - absorbed);
                return _applier.Apply(hit, self, in pre, IsElite, ActiveStatusMask, Position, frame);
            }

            bool annihilation = (hit.Flags & HitFlags.Annihilation) != 0;
            if (hit.IsTick)
            {
                if (!_grace.AllowsTick(frame, hit.SourceId))
                    return Negated(hit, HitResult.Absorbed, frame);
            }
            else if (!annihilation && _grace.IsActive(frame))
            {
                // Gap (06 3.14 does not say): Annihilation is assumed to also bypass grace, like i-frames.
                return Negated(hit, HitResult.Invulnerable, frame);
            }

            InvHitResult inv = Invulnerability.NotifyIncomingHit(frame);
            bool parry = frame <= ParryActiveUntilFrame;
            bool arcValid = Vector3.Dot(Facing, -hitDirection) >= 0f;
            var ctx = new IncomingContext(info.AttackerLevel, _cfg.DifficultyResPenalty, info.IsTelegraphed, arcValid);
            IncomingResult r = _pipeline.Resolve(hit, Stats, Shield.Points, inv != InvHitResult.NotNegated, parry, in ctx);

            if (r.IsNegated)
            {
                var source = new EntityId(hit.SourceId);
                switch (r.Negation)
                {
                    case NegationReason.IFrames:
                        return Negated(hit, inv == InvHitResult.PerfectDodge ? HitResult.PerfectDodged : HitResult.Invulnerable, frame);
                    case NegationReason.Parry:
                        Events?.Publish(new OnParry(frame, hit.Generation, source, self, false));
                        return Negated(hit, HitResult.Parried, frame);
                    default:
                        Events?.Publish(new OnEvade(frame, hit.Generation, source, self));
                        return Negated(hit, HitResult.Evaded, frame);
                }
            }

            DamageOutcome outcome = _applier.Apply(hit, self, in r, IsElite, ActiveStatusMask, Position, frame);
            if (!hit.IsTick) _grace.OnHitLanded(frame, r.AfterDamageReduction, Health.Max);
            return outcome;
        }

        private DamageOutcome Negated(DamageInstance hit, HitResult result, int frame)
        {
            _applier.ReportNegated(hit, Id, result, frame);
            return new DamageOutcome(result, 0f, 0f, 0f, false);
        }
    }
}

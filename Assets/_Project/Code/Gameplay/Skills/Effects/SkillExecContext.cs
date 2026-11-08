using System;
using Eclipse.Core.Damage;
using Eclipse.Core.Procs;
using Eclipse.Core.Random;
using Eclipse.Core.Tags;
using Eclipse.Gameplay.Combat;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>
    /// Per-caster execution state shared by the Deliveries and Effects of a cast (one instance per hero; reused, no allocation per
    /// cast). Holds the services (target query, zones, tethers, resource, events), the running cast and the scratch buffers.
    /// Definitions (Delivery/Effect assets) stay immutable; everything that changes lives here.
    /// </summary>
    public sealed class SkillExecContext
    {
        /// <summary>Distinct targets one strike can gather (engineering cap, same order of magnitude as the M05 area buffers).</summary>
        public const int MaxTargets = 64;

        private struct Gathered
        {
            public AreaCandidate Candidate;
            public int Hits;
            public float DistSqr;
        }

        private readonly IAreaTargetQuery _query;
        private readonly AreaCandidate[] _scratch = new AreaCandidate[MaxTargets];
        private readonly Gathered[] _gathered = new Gathered[MaxTargets];
        private readonly SkillTarget _victim = new SkillTarget();
        private int _gatheredCount;

        public SkillExecContext(SkillTarget caster, IAreaTargetQuery query, SkillDamageCalculator damage, RngStreams rng,
            CombatEventStream events = null)
        {
            Caster = caster ?? throw new ArgumentNullException(nameof(caster));
            _query = query ?? throw new ArgumentNullException(nameof(query));
            Damage = damage ?? throw new ArgumentNullException(nameof(damage));
            Rng = rng ?? throw new ArgumentNullException(nameof(rng));
            Events = events;
        }

        // ---- services ----
        public SkillTarget Caster { get; }
        public SkillDamageCalculator Damage { get; }
        public RngStreams Rng { get; }
        public CombatEventStream Events { get; }
        public ISkillResource Resource { get; set; }
        public ISkillCasterMotion Motion { get; set; }
        public ISkillTargetDirectory Directory { get; set; }
        public ZoneRunner Zones { get; set; }
        public TetherSet Tethers { get; set; }
        /// <summary>Optional: used for <see cref="SkillController.PrepareDamage"/>, <see cref="SkillController.ReportHit"/> and early stance exit.</summary>
        public SkillController Controller { get; set; }
        /// <summary>Weapon Power of the equipped weapon (02 3.4); the base of every skill hit (06 3.2 step 1).</summary>
        public float WeaponPower { get; set; }
        /// <summary>True while the cast button is held (Stance exits when it is released). Written by the input layer each frame.</summary>
        public bool ButtonHeld { get; set; } = true;

        /// <summary>Scratch hit used by Damage effects (the receiver reads it synchronously, never keeps it).</summary>
        public DamageInstance Hit { get; } = new DamageInstance();

        // ---- running cast ----
        public SkillCastInfo Cast { get; set; }
        public int Frame { get; set; }
        /// <summary>0 for a direct cast; Triggers and zones run at the origin Generation + 1 (11 3.6.5).</summary>
        public int Generation { get; set; }
        public HitFlags ExtraFlags { get; set; }
        public SourceChain Chain { get; set; }
        public int HitGroupId { get; set; }
        public SkillEffect[] Effects { get; set; } = Array.Empty<SkillEffect>();
        /// <summary>Multiplies the coefficient of Damage effects (Parry counter +50% sets 1.5). Reset to 1 per cast.</summary>
        public float DamageMultiplier { get; set; } = 1f;
        /// <summary>Flat damage added to the Base of Damage effects (30% of absorbed damage on the Ashen Guard counter). Reset per cast.</summary>
        public float FlatDamage { get; set; }
        public Vector3 ImpactPoint { get; set; }
        /// <summary>How many fissures/lines reached the target an Effect is being applied to (1 for plain areas).</summary>
        public int CurrentHits { get; set; } = 1;
        public int TargetsHit { get; private set; }
        public float DamageDealt { get; private set; }

        // ---- Delivery state (the Delivery assets are shared, so their per-cast state lives here) ----
        public bool InFlight { get; set; }
        public Vector3 PathStart { get; set; }
        public Vector3 PathEnd { get; set; }
        public int PathStartFrame { get; set; }
        public int PathFrames { get; set; }
        public bool StanceActive { get; set; }
        public int StanceStartFrame { get; set; }

        public Vector3 CasterPosition => Caster.Position;
        public Vector3 Forward => Motion != null ? Motion.Forward : Vector3.forward;
        public Vector3 AimPoint => Motion != null ? Motion.AimPoint : Caster.Position + Forward;

        public void BeginCast(in SkillCastInfo cast, int frame, SkillEffect[] effects, int hitGroupId)
        {
            Cast = cast;
            Frame = frame;
            Generation = 0;
            ExtraFlags = HitFlags.None;
            Chain = default;
            HitGroupId = hitGroupId;
            Effects = effects ?? Array.Empty<SkillEffect>();
            DamageMultiplier = 1f;
            FlatDamage = 0f;
            ImpactPoint = Caster.Position;
            CurrentHits = 1;
            TargetsHit = 0;
            DamageDealt = 0f;
            InFlight = false;
            StanceActive = false;
        }

        // ---------------------------------------------------------------- strikes

        /// <summary>Finds targets in one shape and applies the Impact effects. Returns the number of targets reached.</summary>
        public int Strike(in HitboxShape shape, Vector3 origin, Vector3 forward, int maxTargets = 0)
        {
            BeginGather();
            Gather(in shape, origin, forward);
            return Complete(origin, maxTargets);
        }

        public void BeginGather() => _gatheredCount = 0;

        /// <summary>Adds the targets inside a shape; a target already gathered counts one more hit (fissures crossing).</summary>
        public void Gather(in HitboxShape shape, Vector3 origin, Vector3 forward)
        {
            var spec = new AreaSpec(Caster.Id, Caster.Entity != null ? Caster.Entity.Team : Team.Player, shape, origin, forward, 0, 0,
                DamageType.Physical, 0f, 0f, 0f, Generation, HitGroupId, Cast.Spec != null ? Cast.Spec.Tags : TagMask.None, ExtraFlags);
            int n = _query.Collect(in spec, _scratch);
            Vector3 c = Caster.Position;
            for (int i = 0; i < n; i++)
            {
                int existing = -1;
                for (int g = 0; g < _gatheredCount; g++)
                    if (_gathered[g].Candidate.Id == _scratch[i].Id) { existing = g; break; }
                if (existing >= 0) { _gathered[existing].Hits++; continue; }
                if (_gatheredCount >= MaxTargets) continue;
                float dx = _scratch[i].Position.x - c.x, dz = _scratch[i].Position.z - c.z;
                _gathered[_gatheredCount++] = new Gathered { Candidate = _scratch[i], Hits = 1, DistSqr = dx * dx + dz * dz };
            }
        }

        /// <summary>Keeps the closest <paramref name="maxTargets"/> (0 = all), then runs the Impact effects. Returns the number of targets.</summary>
        public int Complete(Vector3 impactPoint, int maxTargets = 0)
        {
            // Insertion sort by distance to the caster: stable, allocation-free, at most MaxTargets items.
            for (int i = 1; i < _gatheredCount; i++)
            {
                Gathered key = _gathered[i];
                int j = i - 1;
                while (j >= 0 && _gathered[j].DistSqr > key.DistSqr) { _gathered[j + 1] = _gathered[j]; j--; }
                _gathered[j + 1] = key;
            }
            int count = maxTargets > 0 && maxTargets < _gatheredCount ? maxTargets : _gatheredCount;
            ImpactPoint = impactPoint;
            for (int i = 0; i < count; i++)
            {
                SkillTarget t = Resolve(in _gathered[i].Candidate);
                CurrentHits = _gathered[i].Hits;
                for (int e = 0; e < Effects.Length; e++)
                {
                    SkillEffect fx = Effects[e];
                    if (fx.Timing == EffectTiming.Impact && fx.Targeting == EffectTargeting.HitTargets) fx.Apply(this, t);
                }
            }
            CurrentHits = 1;
            RunOnce(EffectTiming.Impact);
            return count;
        }

        /// <summary>Effects whose target is the caster or the impact point, for the given timing.</summary>
        public void RunOnce(EffectTiming timing)
        {
            for (int e = 0; e < Effects.Length; e++)
            {
                SkillEffect fx = Effects[e];
                if (fx.Timing != timing) continue;
                if (fx.Targeting == EffectTargeting.Caster) fx.Apply(this, Caster);
                else if (fx.Targeting == EffectTargeting.ImpactPoint) fx.Apply(this, null);
            }
        }

        /// <summary>Applies a list of effects to one target regardless of their timing (Triggers, Tether break).</summary>
        public void ApplyList(SkillEffect[] effects, SkillTarget target)
        {
            if (effects == null) return;
            for (int e = 0; e < effects.Length; e++)
            {
                SkillEffect fx = effects[e];
                switch (fx.Targeting)
                {
                    case EffectTargeting.HitTargets:
                        if (target != null) fx.Apply(this, target);
                        break;
                    case EffectTargeting.Caster:
                        fx.Apply(this, Caster);
                        break;
                    default:
                        fx.Apply(this, null);
                        break;
                }
            }
        }

        /// <summary>Handles of an area candidate: from the directory when present, otherwise the bare receiver reported by the query.</summary>
        public SkillTarget Resolve(in AreaCandidate c)
        {
            if (Directory != null && Directory.TryGet(c.Id, out SkillTarget found) && found != null) return found;
            _victim.Clear();
            _victim.Id = c.Id;
            _victim.Receiver = c.Receiver;
            _victim.FallbackPosition = c.Position;
            return _victim;
        }

        /// <summary>Handles of a runtime id (tethers, triggers); null when unknown.</summary>
        public SkillTarget Resolve(EntityId id)
        {
            return Directory != null && Directory.TryGet(id, out SkillTarget found) ? found : null;
        }

        internal void RecordHit(float damage)
        {
            TargetsHit++;
            DamageDealt += damage;
        }
    }
}

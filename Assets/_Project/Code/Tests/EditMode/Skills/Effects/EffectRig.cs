using System.Collections.Generic;
using Eclipse.Core.Damage;
using Eclipse.Core.Ids;
using Eclipse.Core.Random;
using Eclipse.Core.Stats;
using Eclipse.Core.Tags;
using Eclipse.Data.Skills;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Entities;
using Eclipse.Gameplay.Simulation;
using Eclipse.Gameplay.Skills;
using Eclipse.Tests.EditMode.Gameplay.States;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Tests.EditMode.Skills.Effects
{
    /// <summary>Fake combatant (ICombatEntity mock) with a real Core StatBlock.</summary>
    internal sealed class FakeEntity : ICombatEntity
    {
        public FakeEntity(int id, Team team, Vector3 position)
        {
            Id = new EntityId(id);
            Team = team;
            Position = position;
            Stats = new StatBlock(new StatRuleTable());
            Clock = new EntityClock();
            IsAlive = true;
        }

        public EntityId Id { get; }
        public Team Team { get; }
        public bool IsAlive { get; set; }
        public Vector3 Position { get; set; }
        public EntityClock Clock { get; }
        public StatBlock Stats { get; }
    }

    internal struct HitSnapshot
    {
        public float Total, Physical, Ruin, Ember, Stagger, ProcCoefficient;
        public int Generation, SourceId;
        public HitKind Kind;
        public HitFlags Flags;
        public TagMask Tags;
        public bool IsCrit;
    }

    /// <summary>Fake IDamageReceiver: records what it received, returns the total as final damage.</summary>
    internal sealed class FakeReceiver : IDamageReceiver
    {
        public FakeReceiver(EntityId id) { Id = id; }
        public EntityId Id { get; }
        public HitResult Result = HitResult.Hit;
        public readonly List<HitSnapshot> Hits = new List<HitSnapshot>();

        public DamageOutcome ReceiveHit(DamageInstance hit, Vector3 hitDirection)
        {
            Hits.Add(new HitSnapshot
            {
                Total = hit.TotalDamage, Physical = hit.GetValue(DamageType.Physical), Ruin = hit.GetValue(DamageType.Ruin),
                Ember = hit.GetValue(DamageType.Ember), Stagger = hit.StaggerValue, ProcCoefficient = hit.ProcCoefficient,
                Generation = hit.Generation, SourceId = hit.SourceId, Kind = hit.Kind, Flags = hit.Flags, Tags = hit.Tags, IsCrit = hit.IsCrit,
            });
            return new DamageOutcome(Result, hit.TotalDamage, hit.TotalDamage, 0f, false);
        }
    }

    internal sealed class FakeStatus : IStatusReceiver
    {
        public FakeStatus(EntityId id) { Id = id; }
        public EntityId Id { get; }
        public readonly List<StatusRequest> Requests = new List<StatusRequest>();
        public bool TryApply(in StatusRequest request) { Requests.Add(request); return true; }
        public int GetStacks(StableId status) => 0;
        public void Remove(StableId status) { }
    }

    internal sealed class FakeStaggerable : IStaggerable
    {
        public FakeStaggerable(EntityId id) { Id = id; }
        public EntityId Id { get; }
        public float Added;
        public float StaggerCurrent => Added;
        public float StaggerMax => 100f;
        public bool IsStaggered => false;
        public bool AddStagger(float amount, EntityId source, int generation) { Added += amount; return false; }
    }

    /// <summary>Area query over the registered victims, using the real shape math. Target radius is 1 cm: with exactly 0 float noise on rotated boxes misses points on the centre line.</summary>
    internal sealed class FakeQuery : IAreaTargetQuery
    {
        public const float TargetRadius = 0.01f;
        public readonly List<SkillTarget> Victims = new List<SkillTarget>();
        public readonly List<AreaSpec> Specs = new List<AreaSpec>();

        public int Collect(in AreaSpec spec, AreaCandidate[] buffer)
        {
            Specs.Add(spec);
            int n = 0;
            for (int i = 0; i < Victims.Count && n < buffer.Length; i++)
            {
                SkillTarget v = Victims[i];
                if (!v.Entity.IsAlive || v.Entity.Team == spec.Team) continue;
                if (!ShapeQueries.Overlaps(spec.Shape, spec.Origin, spec.Forward, v.Entity.Position, TargetRadius)) continue;
                buffer[n++] = new AreaCandidate { Id = v.Id, Receiver = v.Receiver, Position = v.Entity.Position };
            }
            return n;
        }
    }

    internal sealed class FakeDirectory : ISkillTargetDirectory
    {
        public readonly Dictionary<int, SkillTarget> Map = new Dictionary<int, SkillTarget>();
        public bool TryGet(EntityId id, out SkillTarget target) => Map.TryGetValue(id.Value, out target);
    }

    internal sealed class FakeMotion : ISkillCasterMotion
    {
        public Vector3 Forward { get; set; } = Vector3.forward;
        public Vector3 AimPoint { get; set; }
        public readonly List<Vector3> Positions = new List<Vector3>();
        public FakeEntity Entity;
        public void SetPosition(Vector3 position)
        {
            Positions.Add(position);
            if (Entity != null) Entity.Position = position;
        }
    }

    internal sealed class FakeResource : ISkillResource
    {
        public float Value;
        public float Gained;
        public float Current => Value;
        public bool PublishesSpentEvent => false;
        public bool TrySpend(float a) { if (a > Value) return false; Value -= a; return true; }
        public float SpendAll() { float v = Value; Value = 0f; return v; }
        public void Gain(float a) { Gained += a; Value += a; }
    }

    /// <summary>Caster, services and victims wired the way the combat registry will wire them. Weapon Power 10 (02 3.4 fixture).</summary>
    internal sealed class EffectRig
    {
        public const float WeaponPower = 10f;
        public static readonly StableId Fury = StableId.Parse("resource.fury");

        public readonly CombatEventStream Events = new CombatEventStream();
        public readonly FakeEntity CasterEntity = new FakeEntity(1, Team.Player, Vector3.zero);
        public readonly SkillTarget Caster = new SkillTarget();
        public readonly FakeQuery Query = new FakeQuery();
        public readonly FakeDirectory Directory = new FakeDirectory();
        public readonly FakeMotion Motion = new FakeMotion();
        public readonly FakeResource Resource = new FakeResource();
        public readonly SkillExecContext Ctx;
        private int _nextId = 10;

        public EffectRig()
        {
            Caster.Id = CasterEntity.Id;
            Caster.Entity = CasterEntity;
            Caster.State = new StateComponent(StatesFixtures.Rules(), CasterEntity.Id, true, null, Events);
            Caster.Shield = new ShieldComponent();
            Caster.Timed = new TimedModifierSet(CasterEntity.Stats);
            Motion.Entity = CasterEntity;
            Directory.Map[Caster.Id.Value] = Caster;

            Ctx = new SkillExecContext(Caster, Query, new SkillDamageCalculator(new OffensiveRules(-0.2f)), new RngStreams(1UL), Events)
            {
                Directory = Directory, Motion = Motion, Resource = Resource, WeaponPower = WeaponPower,
            };
        }

        public static SkillSpec Spec(string id, string tags, float coefficient, SkillFrames frames, params (string key, float value)[] ps)
        {
            var b = new SkillSpecBuilder
            {
                SkillId = StableId.Parse(id), Slot = SkillSlot.Q, Tags = TagMask.Parse(tags), DamageCoefficient = coefficient, Frames = frames,
            };
            foreach (var p in ps) b.SetParam(p.key, p.value);
            return b.Build(null);
        }

        public static SkillSpec Spec(float coefficient = 2.2f, params (string key, float value)[] ps) =>
            Spec("skill.test.fx", "Attack|Melee|Area|Physical", coefficient, new SkillFrames(3, 2, 4), ps);

        /// <summary>Starts a cast context for <paramref name="spec"/> at <paramref name="frame"/>.</summary>
        public void Begin(SkillSpec spec, int frame = 100, SkillEffect[] effects = null)
        {
            var info = new SkillCastInfo(spec, frame, 0f, spec.DamageCoefficient);
            Ctx.BeginCast(in info, frame, effects, 77);
            Ctx.Frame = frame;
        }

        public SkillTarget AddEnemy(float x, float z, bool withState = true)
        {
            int id = _nextId++;
            var e = new FakeEntity(id, Team.Enemy, new Vector3(x, 0f, z));
            var t = new SkillTarget
            {
                Id = e.Id, Entity = e, Receiver = new FakeReceiver(e.Id), Status = new FakeStatus(e.Id),
                Stagger = new FakeStaggerable(e.Id), Shield = new ShieldComponent(), Timed = new TimedModifierSet(e.Stats),
                Vulnerability = new TimedVulnerabilitySet(),
            };
            if (withState) t.State = new StateComponent(StatesFixtures.Rules(), e.Id, false, new FakeMass(MassClass.Medium), Events);
            Query.Victims.Add(t);
            Directory.Map[id] = t;
            return t;
        }

        public static FakeReceiver Rx(SkillTarget t) => (FakeReceiver)t.Receiver;
        public static FakeStatus St(SkillTarget t) => (FakeStatus)t.Status;
    }
}

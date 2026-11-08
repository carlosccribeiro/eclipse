using Eclipse.Core.Damage;
using Eclipse.Core.Random;
using Eclipse.Core.Stats;
using Eclipse.Core.Tags;
using Eclipse.Gameplay.Combat;
using NUnit.Framework;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;
using Flask = Eclipse.Gameplay.Player.AetherFlask;
using FlaskConfig = Eclipse.Gameplay.Player.AetherFlaskConfig;

namespace Eclipse.Tests.EditMode.Gameplay.Damage
{
    /// <summary>Fixtures mirror the doc values (06 3.3, 3.12, 3.14; 01 3.4.1); runtime code receives them as injected config.</summary>
    internal static class Fx
    {
        // Real caps (01): armor 75%, phys res 30%, res 75% / -50%, dodge 40%, block 50%, DR 40%.
        public static readonly MitigationRules Caps = new MitigationRules(0.75f, 0.30f, 0.75f, -0.50f, 0.40f, 0.50f, 0.40f);
        // Same, but Dodge/Block caps lifted so a 100% chance is deterministic.
        public static readonly MitigationRules Sure = new MitigationRules(0.75f, 0.30f, 0.75f, -0.50f, 1f, 1f, 0.40f);

        public static CombatEntityConfig Config(MitigationRules rules) => new CombatEntityConfig
        {
            Mitigation = rules,
            BlockAmount = 0.40f,
            DifficultyResPenalty = 0.25f,
            GraceFrames = CombatEntityConfig.SecondsToFrames(0.25f),       // 15
            HeavyGraceFrames = CombatEntityConfig.SecondsToFrames(0.8f),   // 48
            HeavyHitHealthFraction = 0.20f,
            TickIntervalFrames = CombatEntityConfig.SecondsToFrames(0.5f), // 30
            LowHealthFraction = 0.30f,
            DeathSlowMotionScale = 0.3f,
            DeathSlowMotionFrames = CombatEntityConfig.SecondsToFrames(0.5f),
            ReviveHealthFraction = 0.5f,
            ReviveInvulnerabilityFrames = CombatEntityConfig.SecondsToFrames(3f),
            MaxRevivesPerBossRoom = 1,
        };

        public static CombatEntity Player(CombatEventStream ev, MitigationRules rules, float health = 1000f, Team team = Team.Player)
        {
            var stats = new StatBlock(new StatRuleTable());
            stats.SetBase(StatId.MaximumHealth, health);
            return new CombatEntity(new EntityId(1), team, stats, Config(rules), ev, new RngStreams(7UL));
        }

        public static DamageInstance Hit(float physical, HitFlags flags = HitFlags.None, HitKind kind = HitKind.Hit, int source = 50)
        {
            var h = new DamageInstance { SourceId = source, Flags = flags, Kind = kind };
            h.AddComponent(DamageType.Physical, physical);
            return h;
        }

        // Hit travelling towards -Z hits an entity facing +Z in the front arc.
        public static readonly Vector3 FromFront = new Vector3(0f, 0f, -1f);
        public static readonly Vector3 FromBack = new Vector3(0f, 0f, 1f);
    }

    public class IncomingDamageTests
    {
        private CombatEventStream _ev;
        private CombatEntity _p;

        [SetUp]
        public void SetUp()
        {
            _ev = new CombatEventStream(64);
            _p = Fx.Player(_ev, Fx.Sure);
            _p.Facing = Vector3.forward;
        }

        private DamageOutcome Receive(DamageInstance h, bool telegraphed = true, int frame = 100, Vector3? dir = null)
        {
            _p.CurrentFrame = frame;
            return _p.ReceiveHit(h, dir ?? Fx.FromFront, new IncomingHitInfo(0f, telegraphed));
        }

        // ---- Step 0: i-frames / Parry ----

        [Test]
        public void IFrames_NegateHit_AndUnavoidable_ButNotAnnihilation()
        {
            _p.Invulnerability.AddWindow(90, 200);
            Assert.AreEqual(HitResult.Invulnerable, Receive(Fx.Hit(100f)).Result);
            Assert.AreEqual(HitResult.Invulnerable, Receive(Fx.Hit(100f, HitFlags.Unavoidable), frame: 120).Result);
            var o = Receive(Fx.Hit(100f, HitFlags.Annihilation), frame: 140);
            Assert.AreEqual(HitResult.Hit, o.Result);
            Assert.AreEqual(100f, o.HealthDamage, 0.001f);
        }

        [Test]
        public void IFrames_PerfectWindow_ReportsPerfectDodged()
        {
            _p.Invulnerability.AddWindow(100, 111, 6);
            Assert.AreEqual(HitResult.PerfectDodged, Receive(Fx.Hit(100f), frame: 102).Result);
            Assert.AreEqual(HitResult.Invulnerable, Receive(Fx.Hit(100f), frame: 109).Result);
        }

        [Test]
        public void Parry_Negates_ButNotUnblockableNorAnnihilation()
        {
            _p.ParryActiveUntilFrame = 1000;
            Assert.AreEqual(HitResult.Parried, Receive(Fx.Hit(100f), frame: 100).Result);
            Assert.AreEqual(1, _ev.Count<OnParry>());
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(100f, HitFlags.Unblockable), frame: 200).Result);
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(100f, HitFlags.Annihilation), frame: 300).Result);
        }

        [Test]
        public void Parry_StillNegatesUnavoidable()
        {
            _p.ParryActiveUntilFrame = 1000;
            Assert.AreEqual(HitResult.Parried, Receive(Fx.Hit(100f, HitFlags.Unavoidable)).Result);
        }

        // ---- Step 2: Dodge Chance ----

        [Test]
        public void DodgeChance_EvadesUntelegraphedHit_AndPublishesOnEvade()
        {
            _p.Stats.SetBase(StatId.DodgeChance, 1f);
            var o = Receive(Fx.Hit(100f), telegraphed: false);
            Assert.AreEqual(HitResult.Evaded, o.Result);
            Assert.AreEqual(0f, o.HealthDamage);
            Assert.AreEqual(1, _ev.Count<OnEvade>());
        }

        [Test]
        public void DodgeChance_DoesNotApply_ToTelegraphed_Unavoidable_Annihilation_OrTick()
        {
            _p.Stats.SetBase(StatId.DodgeChance, 1f);
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(100f), telegraphed: true, frame: 100).Result);
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(100f, HitFlags.Unavoidable), false, 200).Result);
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(100f, HitFlags.Annihilation), false, 300).Result);
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(10f, kind: HitKind.Tick), false, 400).Result);
            Assert.AreEqual(0, _ev.Count<OnEvade>());
        }

        // ---- Step 3: mitigation ----

        [Test]
        public void Mitigation_PhysicalUsesArmorWithK()
        {
            _p.Stats.SetBase(StatId.Armor, 500f); // K = 50*0 + 500 -> 50%
            var o = Receive(Fx.Hit(100f));
            Assert.AreEqual(50f, o.FinalDamage, 0.001f);
        }

        [Test]
        public void Mitigation_ElementalAppliesDifficultyPenalty()
        {
            _p.Stats.SetBase(StatId.ElementalResistance, 0.5f); // 0.5 - 0.25 penalty = 0.25
            var h = new DamageInstance { SourceId = 50 };
            h.AddComponent(DamageType.Ember, 100f);
            Assert.AreEqual(75f, Receive(h).FinalDamage, 0.001f);
        }

        // ---- Step 4: Block ----

        [Test]
        public void Block_Reduces40Percent_AndPublishesOnBlock()
        {
            _p.Stats.SetBase(StatId.BlockChance, 1f);
            var o = Receive(Fx.Hit(100f));
            Assert.AreEqual(HitResult.Blocked, o.Result);
            Assert.AreEqual(60f, o.FinalDamage, 0.001f);
            Assert.AreEqual(1, _ev.Count<OnBlock>());
            Assert.AreEqual(40f, _ev.Read<OnBlock>()[0].Value, 0.001f);
        }

        [Test]
        public void Block_DoesNotApply_ToUnblockable_Unavoidable_Annihilation_Tick_OrBackArc()
        {
            _p.Stats.SetBase(StatId.BlockChance, 1f);
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(100f, HitFlags.Unblockable), frame: 100).Result);
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(100f, HitFlags.Unavoidable), frame: 200).Result);
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(100f, HitFlags.Annihilation), frame: 300).Result);
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(10f, kind: HitKind.Tick), frame: 400).Result);
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(100f), frame: 500, dir: Fx.FromBack).Result);
            Assert.AreEqual(0, _ev.Count<OnBlock>());
        }

        // ---- Step 5: Damage Reduction ----

        [Test]
        public void DamageReduction_IsCappedAt40Percent()
        {
            _p.Stats.SetBase(StatId.DamageReduction, 0.9f);
            Assert.AreEqual(60f, Receive(Fx.Hit(100f)).FinalDamage, 0.001f);
        }

        [Test]
        public void Order_MitigationThenBlockThenDamageReductionThenShieldThenHealth()
        {
            _p.Stats.SetBase(StatId.Armor, 500f);          // 100 -> 50
            _p.Stats.SetBase(StatId.BlockChance, 1f);      // 50 -> 30
            _p.Stats.SetBase(StatId.DamageReduction, 0.4f);// 30 -> 18
            _p.Shield.Grant(8f);                            // 18 -> shield 8, health 10
            var o = Receive(Fx.Hit(100f));
            Assert.AreEqual(18f, o.FinalDamage, 0.001f);
            Assert.AreEqual(10f, o.HealthDamage, 0.001f);
            Assert.AreEqual(990f, _p.Health.Current, 0.001f);
            Assert.AreEqual(0f, _p.Shield.Points);
        }

        // ---- Step 6-7: Shield and Health ----

        [Test]
        public void Shield_AbsorbsBeforeHealth_AndPublishesOnShieldBroken()
        {
            _p.Shield.Grant(30f);
            var o = Receive(Fx.Hit(100f));
            Assert.AreEqual(70f, o.HealthDamage, 0.001f);
            Assert.AreEqual(930f, _p.Health.Current, 0.001f);
            Assert.AreEqual(1, _ev.Count<OnShieldBroken>());
        }

        [Test]
        public void Shield_PartialAbsorb_DoesNotBreak()
        {
            _p.Shield.Grant(300f);
            var o = Receive(Fx.Hit(100f));
            Assert.AreEqual(0f, o.HealthDamage);
            Assert.AreEqual(200f, _p.Shield.Points, 0.001f);
            Assert.AreEqual(0, _ev.Count<OnShieldBroken>());
        }

        [Test]
        public void ShieldStrength_AmplifiesGrantedShield()
        {
            _p.Shield.Grant(100f, 0.25f);
            Assert.AreEqual(125f, _p.Shield.Points, 0.001f);
        }

        [Test]
        public void Hit_EmitsOnHitTaken_WithHealthLostAndResult()
        {
            Receive(Fx.Hit(100f));
            Assert.AreEqual(1, _ev.Count<OnHitTaken>());
            var e = _ev.Read<OnHitTaken>()[0];
            Assert.AreEqual(100f, e.Value, 0.001f);
            Assert.AreEqual(HitResult.Hit, e.Result);
            Assert.AreEqual(new EntityId(50), e.Source);
        }

        [Test]
        public void LowHealth_FiresOnlyWhenCrossingThreshold()
        {
            Receive(Fx.Hit(600f), frame: 100); // 400/1000 = 0.4 >= 0.3
            Assert.AreEqual(0, _ev.Count<OnLowHealth>());
            Receive(Fx.Hit(200f), frame: 300); // 0.2 < 0.3
            Assert.AreEqual(1, _ev.Count<OnLowHealth>());
            Receive(Fx.Hit(50f), frame: 500);
            Assert.AreEqual(1, _ev.Count<OnLowHealth>());
        }

        [Test]
        public void Kill_PublishesDeathKillOverkill_AndMarksDead()
        {
            var o = Receive(Fx.Hit(1300f));
            Assert.IsTrue(o.Killed);
            Assert.AreEqual(300f, o.Overkill, 0.001f);
            Assert.IsFalse(_p.IsAlive);
            Assert.AreEqual(1, _ev.Count<OnDeath>());
            Assert.AreEqual(1, _ev.Count<OnKill>());
            Assert.AreEqual(1, _ev.Count<OnOverkill>());
        }

        [Test]
        public void DeadEntity_IgnoresFurtherHits()
        {
            Receive(Fx.Hit(1300f), frame: 100);
            var o = Receive(Fx.Hit(10f), frame: 400);
            Assert.AreEqual(HitResult.Invulnerable, o.Result);
            Assert.AreEqual(1, _ev.Count<OnDeath>());
        }

        [Test]
        public void EnemyTeam_ReceivesPreMitigatedDamage_NoPlayerDefenses()
        {
            var e = Fx.Player(_ev, Fx.Sure, 500f, Team.Enemy);
            e.Stats.SetBase(StatId.Armor, 500f);
            e.Stats.SetBase(StatId.BlockChance, 1f);
            e.CurrentFrame = 10;
            var o = e.ReceiveHit(Fx.Hit(100f), Fx.FromFront);
            Assert.AreEqual(100f, o.FinalDamage, 0.001f);
            Assert.AreEqual(400f, e.Health.Current, 0.001f);
            Assert.AreEqual(HitResult.Hit, o.Result);
        }

        [Test]
        public void Pipeline_DodgeRollsAreDeterministicPerSeed()
        {
            // Same seed, same hits -> same dodge outcomes (Combat stream).
            var a = Fx.Player(new CombatEventStream(16), Fx.Caps);
            var b = Fx.Player(new CombatEventStream(16), Fx.Caps);
            a.Stats.SetBase(StatId.DodgeChance, 0.3f);
            b.Stats.SetBase(StatId.DodgeChance, 0.3f);
            for (int i = 0; i < 20; i++)
            {
                a.CurrentFrame = b.CurrentFrame = i * 100;
                var ra = a.ReceiveHit(Fx.Hit(1f), Fx.FromFront, new IncomingHitInfo(0f, false)).Result;
                var rb = b.ReceiveHit(Fx.Hit(1f), Fx.FromFront, new IncomingHitInfo(0f, false)).Result;
                Assert.AreEqual(ra, rb);
            }
        }
    }

    public class GraceAndDeathTests
    {
        private CombatEventStream _ev;
        private CombatEntity _p;

        [SetUp]
        public void SetUp()
        {
            _ev = new CombatEventStream(64);
            _p = Fx.Player(_ev, Fx.Sure);
        }

        private DamageOutcome Receive(DamageInstance h, int frame)
        {
            _p.CurrentFrame = frame;
            return _p.ReceiveHit(h, Fx.FromFront, new IncomingHitInfo(0f, true));
        }

        [Test]
        public void Grace_AfterNormalHit_Lasts15Frames()
        {
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(10f), 100).Result);
            Assert.AreEqual(HitResult.Invulnerable, Receive(Fx.Hit(10f), 101).Result);
            Assert.AreEqual(HitResult.Invulnerable, Receive(Fx.Hit(10f), 114).Result);
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(10f), 115).Result);
            Assert.AreEqual(980f, _p.Health.Current, 0.001f);
        }

        [Test]
        public void Grace_AfterHeavyHit_Lasts48Frames()
        {
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(200f), 100).Result); // exactly 20% of 1000
            Assert.AreEqual(HitResult.Invulnerable, Receive(Fx.Hit(10f), 147).Result);
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(10f), 148).Result);
        }

        [Test]
        public void Grace_JustBelowHeavyThreshold_UsesShortGrace()
        {
            Receive(Fx.Hit(199f), 100);
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(10f), 115).Result);
        }

        [Test]
        public void Grace_NotStartedByNegatedHit()
        {
            _p.Invulnerability.AddWindow(100, 100);
            Assert.AreEqual(HitResult.Invulnerable, Receive(Fx.Hit(10f), 100).Result);
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(10f), 101).Result);
        }

        [Test]
        public void Ticks_DoNotStartGrace_AndAreNotBlockedByGrace()
        {
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(5f, kind: HitKind.Tick, source: 9), 100).Result);
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(10f), 101).Result); // no grace from the tick
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(5f, kind: HitKind.Tick, source: 8), 102).Result); // inside hit grace
        }

        [Test]
        public void Ticks_LimitedToOnePerSourcePerHalfSecond()
        {
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(5f, kind: HitKind.Tick, source: 9), 100).Result);
            Assert.AreEqual(HitResult.Absorbed, Receive(Fx.Hit(5f, kind: HitKind.Tick, source: 9), 129).Result);
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(5f, kind: HitKind.Tick, source: 7), 129).Result); // other source
            Assert.AreEqual(HitResult.Hit, Receive(Fx.Hit(5f, kind: HitKind.Tick, source: 9), 130).Result);
            Assert.AreEqual(985f, _p.Health.Current, 0.001f);
        }

        [Test]
        public void GracePeriod_Unit_TickSourceTableEvictsOldest()
        {
            var g = new GracePeriod(Fx.Config(Fx.Sure));
            for (int s = 1; s <= 20; s++) Assert.IsTrue(g.AllowsTick(100 + s, s));
            Assert.IsTrue(g.AllowsTick(130, 1)); // evicted: treated as new
        }

        [Test]
        public void DeathRecap_KeepsLast5Hits_WithSourceTypeValueAndMitigation()
        {
            _p.Stats.SetBase(StatId.Armor, 500f); // 50%
            _p.ActiveStatusMask = 0b101;
            for (int i = 1; i <= 7; i++) Receive(Fx.Hit(10f * i, source: 100 + i), i * 100);
            var r = _p.Recap;
            Assert.AreEqual(DeathRecap.Capacity, r.Count);
            Assert.AreEqual(5, DeathRecap.Capacity);
            var oldest = r.Get(0);
            var newest = r.Get(4);
            Assert.AreEqual(new EntityId(103), oldest.Source);
            Assert.AreEqual(30f, oldest.Incoming, 0.001f);
            Assert.AreEqual(15f, oldest.Final, 0.001f);
            Assert.AreEqual(15f, oldest.Mitigated, 0.001f);
            Assert.AreEqual(DamageType.Physical, oldest.Type);
            Assert.AreEqual(0b101UL, oldest.ActiveStatuses);
            Assert.AreEqual(new EntityId(107), newest.Source);
            Assert.AreEqual(700, newest.Frame);
        }

        [Test]
        public void DeathRecap_NegatedHitsAreNotRecorded()
        {
            _p.Invulnerability.AddWindow(0, 1000);
            Receive(Fx.Hit(10f), 100);
            Assert.AreEqual(0, _p.Recap.Count);
        }

        [Test]
        public void Death_SlowMotionOnlyForHalfSecond_PresentationOnly()
        {
            Receive(Fx.Hit(5000f), 100);
            Assert.AreEqual(0.3f, _p.Death.TimeScale(100), 1e-6f);
            Assert.AreEqual(0.3f, _p.Death.TimeScale(129), 1e-6f);
            Assert.AreEqual(1f, _p.Death.TimeScale(130), 1e-6f);
        }

        [Test]
        public void Revive_RestoresHalfHealth_Grants3sInvulnerability_AndCapsPerBossRoom()
        {
            Assert.IsFalse(_p.Death.TryRevive(_p.Health, _p.Invulnerability, _p.Id, 50), "alive: no revive");
            Receive(Fx.Hit(5000f), 100);
            Assert.IsTrue(_p.Death.TryRevive(_p.Health, _p.Invulnerability, _p.Id, 200));
            Assert.IsTrue(_p.IsAlive);
            Assert.AreEqual(500f, _p.Health.Current, 0.001f);
            Assert.IsTrue(_p.Invulnerability.IsInvulnerable(200));
            Assert.IsTrue(_p.Invulnerability.IsInvulnerable(379));
            Assert.IsFalse(_p.Invulnerability.IsInvulnerable(380));
            Assert.AreEqual(1, _ev.Count<OnRevive>());

            Receive(Fx.Hit(5000f), 400);
            Assert.IsFalse(_p.Death.TryRevive(_p.Health, _p.Invulnerability, _p.Id, 500), "cap 1 per boss room");
            _p.Death.ResetBossRoom();
            Assert.IsTrue(_p.Death.TryRevive(_p.Health, _p.Invulnerability, _p.Id, 500));
        }
    }

    public class AetherFlaskTests
    {
        private CombatEventStream _ev;
        private CombatEntity _p;
        private Flask _flask;

        [SetUp]
        public void SetUp()
        {
            _ev = new CombatEventStream(64);
            _p = Fx.Player(_ev, Fx.Sure);
            _flask = new Flask(new FlaskConfig { MaxCharges = 3, HealFraction = 0.35f, HealFrames = 60 }, _p);
        }

        private void Drain(int ticks) { for (int i = 0; i < ticks; i++) _flask.Tick(); }

        [Test]
        public void Starts_With3Charges()
        {
            Assert.AreEqual(3, _flask.Charges);
        }

        [Test]
        public void Use_Heals35PercentOver60Frames_AndConsumesCharge()
        {
            _p.Health.Damage(800f, out _);
            Assert.IsTrue(_flask.TryUse(10));
            Assert.AreEqual(2, _flask.Charges);
            Drain(30);
            Assert.AreEqual(200f + 175f, _p.Health.Current, 0.01f);
            Drain(30);
            Assert.AreEqual(200f + 350f, _p.Health.Current, 0.01f);
            Assert.IsFalse(_flask.IsHealing);
            Assert.AreEqual(1, _ev.Count<OnFlaskUsed>());
            Assert.AreEqual(2, _ev.Read<OnFlaskUsed>()[0].ChargesLeft);
        }

        [Test]
        public void HealingEffectiveness_AmplifiesHeal()
        {
            _p.Stats.SetBase(StatId.HealingEffectiveness, 0.2f);
            _p.Health.Damage(900f, out _);
            _flask.TryUse(1);
            Drain(60);
            Assert.AreEqual(100f + 420f, _p.Health.Current, 0.01f);
        }

        [Test]
        public void Heal_IsNotCancelledByDamage()
        {
            _p.Health.Damage(500f, out _);
            _flask.TryUse(1);
            Drain(20);
            _p.CurrentFrame = 500;
            _p.ReceiveHit(Fx.Hit(50f), Fx.FromFront, new IncomingHitInfo(0f, true));
            Assert.IsTrue(_flask.IsHealing);
            Drain(40);
            Assert.AreEqual(500f - 50f + 350f, _p.Health.Current, 0.01f);
        }

        [Test]
        public void CannotUse_DuringHeavyOrUltimate_NoCharges_OrWhileHealing_OrDead()
        {
            _flask.HeavyOrUltimateActive = true;
            Assert.IsFalse(_flask.TryUse(1));
            Assert.AreEqual(3, _flask.Charges);
            _flask.HeavyOrUltimateActive = false;
            Assert.IsTrue(_flask.TryUse(1));
            Assert.IsFalse(_flask.TryUse(2), "still healing");
            Drain(60);
            Assert.IsTrue(_flask.TryUse(3));
            Drain(60);
            Assert.IsTrue(_flask.TryUse(4));
            Drain(60);
            Assert.IsFalse(_flask.TryUse(5), "no charges");
            _flask.RestoreAll();
            _p.CurrentFrame = 9;
            _p.ReceiveHit(Fx.Hit(5000f), Fx.FromFront, new IncomingHitInfo(0f, true));
            Assert.IsFalse(_flask.TryUse(6), "dead");
        }

        [Test]
        public void EliteKill_RefundsOneCharge_MaxOncePerRoom()
        {
            _flask.TryUse(1); Drain(60);
            _flask.TryUse(2); Drain(60);
            Assert.AreEqual(1, _flask.Charges);
            var elite = new OnKill(5, 0, _p.Id, new EntityId(77), 10f, true, Vector3.zero);
            Assert.IsTrue(_flask.HandleKill(elite, 4));
            Assert.AreEqual(2, _flask.Charges);
            Assert.IsFalse(_flask.HandleKill(elite, 4), "second elite in same room");
            Assert.AreEqual(2, _flask.Charges);
            Assert.IsTrue(_flask.HandleKill(elite, 5), "next room");
            Assert.AreEqual(3, _flask.Charges);
            Assert.IsFalse(_flask.HandleKill(elite, 6), "already full");
        }

        [Test]
        public void NonEliteKill_OrKillByOther_DoesNotRefund()
        {
            _flask.TryUse(1); Drain(60);
            var normal = new OnKill(5, 0, _p.Id, new EntityId(77), 10f, false, Vector3.zero);
            var other = new OnKill(5, 0, new EntityId(999), new EntityId(77), 10f, true, Vector3.zero);
            Assert.IsFalse(_flask.HandleKill(normal, 1));
            Assert.IsFalse(_flask.HandleKill(other, 1));
            Assert.AreEqual(2, _flask.Charges);
        }

        [Test]
        public void Kill_OfEliteEnemyEntity_PublishesOnKillWithEliteFlag()
        {
            var enemy = Fx.Player(_ev, Fx.Sure, 100f, Team.Enemy);
            enemy.IsElite = true;
            var hit = Fx.Hit(500f, source: _p.Id.Value);
            enemy.ReceiveHit(hit, Fx.FromFront);
            Assert.AreEqual(1, _ev.Count<OnKill>());
            Assert.IsTrue(_ev.Read<OnKill>()[0].TargetIsElite);
        }
    }
}

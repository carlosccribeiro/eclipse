using Eclipse.Core.CrowdControl;
using Eclipse.Core.Tags;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Entities;
using Eclipse.Gameplay.Player;
using NUnit.Framework;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Tests.EditMode.Gameplay.States
{
    public sealed class StateComponentTests
    {
        private CombatEventStream _ev;
        private CombatStateRules _rules;
        private static readonly EntityId Self = new EntityId(1);
        private static readonly EntityId Src = new EntityId(2);

        [SetUp]
        public void SetUp()
        {
            _ev = new CombatEventStream(64);
            _rules = StatesFixtures.Rules();
        }

        private StateComponent Enemy(MassClass mass = MassClass.Medium, bool elite = false, bool guardianOrBoss = false) =>
            new StateComponent(_rules, Self, false, new FakeMass(mass, elite), _ev, guardianOrBoss);

        private StateComponent Player() => new StateComponent(_rules, Self, true, null, _ev);

        // ---------------------------------------------------------------- CC and DR

        [Test]
        public void EnemyDr_100_50_25_Immune_Within8Seconds()
        {
            var s = Enemy();
            Assert.AreEqual(120, s.ApplyCc(CcKind.Stun, 2f, Src, 0, 0).DurationFrames);
            Assert.AreEqual(60, s.ApplyCc(CcKind.Stun, 2f, Src, 10, 0).DurationFrames);
            Assert.AreEqual(30, s.ApplyCc(CcKind.Stun, 2f, Src, 20, 0).DurationFrames);
            var fourth = s.ApplyCc(CcKind.Stun, 2f, Src, 30, 0);
            Assert.AreEqual(CcOutcome.ImmuneDr, fourth.Outcome);
            Assert.AreEqual(0, fourth.DurationFrames);
            Assert.AreEqual(3, _ev.Count<OnCCApplied>());
            // Window ends 8 s (480 f) after the first CC.
            Assert.AreEqual(CcOutcome.ImmuneDr, s.ApplyCc(CcKind.Stun, 2f, Src, 479, 0).Outcome);
            Assert.AreEqual(120, s.ApplyCc(CcKind.Stun, 2f, Src, 480, 0).DurationFrames);
        }

        [Test]
        public void StunAndFreeze_ShareHardGroup_RootHasOwn()
        {
            var s = Enemy();
            Assert.AreEqual(60, s.ApplyCc(CcKind.Freeze, 1f, Src, 0, 0).DurationFrames);
            Assert.AreEqual(30, s.ApplyCc(CcKind.Stun, 1f, Src, 1, 0).DurationFrames); // 2nd of Hard = 50%
            Assert.AreEqual(60, s.ApplyCc(CcKind.Root, 1f, Src, 2, 0).DurationFrames); // Root group is independent
        }

        [Test]
        public void Player_Caps_HardFreezeRoot_AndImmunity()
        {
            var s = Player();
            Assert.AreEqual(90, s.ApplyCc(CcKind.Stun, 3f, Src, 0, 0).DurationFrames);   // 1.5 s
            Assert.IsTrue(s.IsStunned(89));
            Assert.AreEqual(CcOutcome.ImmuneDr, s.ApplyCc(CcKind.Stun, 1f, Src, 100, 0).Outcome); // 2 s immunity after the end (90..210)
            Assert.AreEqual(CcOutcome.ImmuneDr, s.ApplyCc(CcKind.Freeze, 1f, Src, 209, 0).Outcome);
            Assert.AreEqual(60, s.ApplyCc(CcKind.Freeze, 3f, Src, 210, 0).DurationFrames); // Freeze max 1.0 s
            Assert.AreEqual(120, s.ApplyCc(CcKind.Root, 5f, Src, 215, 0).DurationFrames);  // Root max 2 s
        }

        [Test]
        public void Player_StatusResistance_ReducesDuration()
        {
            Assert.AreEqual(30, Player().ApplyCc(CcKind.Stun, 1f, Src, 0, 0, 0.5f).DurationFrames);
        }

        [Test]
        public void Player_NeverTaunted_AndGuardianBossImmuneToTaunt()
        {
            Assert.AreEqual(CcOutcome.ImmuneRole, Player().ApplyCc(CcKind.Taunt, 2f, Src, 0, 0).Outcome);
            Assert.AreEqual(CcOutcome.ImmuneRole, Enemy(MassClass.Heavy, false, true).ApplyCc(CcKind.Taunt, 2f, Src, 0, 0).Outcome);
        }

        [Test]
        public void Taunt_CappedAt3s_EliteHalf()
        {
            Assert.AreEqual(180, Enemy().ApplyCc(CcKind.Taunt, 5f, Src, 0, 0).DurationFrames);
            Assert.AreEqual(90, Enemy(MassClass.Medium, true).ApplyCc(CcKind.Taunt, 5f, Src, 0, 0).DurationFrames);
        }

        [Test]
        public void Elite_HardCcDuration_x06()
        {
            Assert.AreEqual(72, Enemy(MassClass.Medium, true).ApplyCc(CcKind.Stun, 2f, Src, 0, 0).DurationFrames);
        }

        [Test]
        public void Root_BlocksMoveAndDodge_ButNotCast_Silence_BlocksCast()
        {
            var s = Enemy();
            s.ApplyCc(CcKind.Root, 1f, Src, 0, 0);
            Assert.IsFalse(s.CanMove(10));
            Assert.IsFalse(s.CanDodge(10));
            Assert.IsTrue(s.CanCastSkills(10));
            Assert.IsTrue(s.CanAct(10));
            Assert.IsTrue(s.CanMove(60));
            var m = Enemy();
            m.ApplyCc(CcKind.Silence, 1f, Src, 0, 0);
            Assert.IsFalse(m.CanCastSkills(10));
        }

        [Test]
        public void Unstoppable_BlocksAllCc_AndSlow_AndKnockback()
        {
            var s = Enemy();
            s.GrantUnstoppable(100);
            foreach (CcKind k in new[] { CcKind.Stun, CcKind.Freeze, CcKind.Root, CcKind.Silence, CcKind.Taunt })
                Assert.AreEqual(CcOutcome.BlockedUnstoppable, s.ApplyCc(k, 1f, Src, 10, 0).Outcome, k.ToString());
            Assert.IsFalse(s.ApplySlow(0.3f, 0f, 2f, 10));
            Assert.AreEqual(0f, s.ApplyKnockback(5f, 0.5f, Src, 10, 0).Amount);
            Assert.AreEqual(0f, s.ApplyPull(5f, 10).Amount);
            Assert.AreEqual(0, _ev.Count<OnCCApplied>());
            // After it ends the CC lands again (and DR was not consumed by the blocked attempts).
            Assert.AreEqual(60, s.ApplyCc(CcKind.Stun, 1f, Src, 100, 0).DurationFrames);
        }

        [Test]
        public void Colossal_ImmuneToCc_ConvertsStunRootFreezeToStagger()
        {
            var st = Enemy(MassClass.Colossal, false, true);
            var stagger = new StaggerComponent(_rules, Self, StaggerTier.Boss, false, 1f, _ev, st);
            st.SetStagger(stagger);
            stagger.Tick(0);
            var stun = st.ApplyCc(CcKind.Stun, 2f, Src, 0, 0);
            Assert.AreEqual(CcOutcome.ConvertedToStagger, stun.Outcome);
            Assert.AreEqual(80f, stun.ConvertedStagger, 1e-4f);
            Assert.AreEqual(80f, stagger.StaggerCurrent, 1e-4f);
            Assert.IsFalse(st.IsStunned(1));
            var freeze = st.ApplyCc(CcKind.Freeze, 1f, Src, 0, 0);
            Assert.IsTrue(freeze.FrostboundHook);
            Assert.AreEqual(40f, freeze.ConvertedStagger, 1e-4f);
            Assert.AreEqual(120f, stagger.StaggerCurrent, 1e-4f);
            Assert.AreEqual(CcOutcome.ImmuneRole, st.ApplyCc(CcKind.Silence, 1f, Src, 0, 0).Outcome);
            Assert.AreEqual(0, _ev.Count<OnCCApplied>());
        }

        // ---------------------------------------------------------------- Displacement

        [Test]
        public void Knockback_ScaledByMassClass_AndElite()
        {
            Assert.AreEqual(10f, Enemy(MassClass.Light).ApplyKnockback(10f, 0f, Src, 0, 0).Amount, 1e-4f);
            Assert.AreEqual(7f, Enemy(MassClass.Medium).ApplyKnockback(10f, 0f, Src, 0, 0).Amount, 1e-4f);
            Assert.AreEqual(3f, Enemy(MassClass.Heavy).ApplyKnockback(10f, 0f, Src, 0, 0).Amount, 1e-4f);
            Assert.AreEqual(4.2f, Enemy(MassClass.Medium, true).ApplyKnockback(10f, 0f, Src, 0, 0).Amount, 1e-4f);
            Assert.AreEqual(0f, Enemy(MassClass.Colossal).ApplyKnockback(10f, 0f, Src, 0, 0).Amount);
        }

        [Test]
        public void Pull_OnColossal_RaisesDistortionHook()
        {
            var r = Enemy(MassClass.Colossal).ApplyPull(10f, 0);
            Assert.IsTrue(r.DistortionHook);
            Assert.AreEqual(0f, r.Amount);
            Assert.AreEqual(7f, Enemy(MassClass.Medium).ApplyPull(10f, 0).Amount, 1e-4f);
        }

        [Test]
        public void WallImpact_10PercentDamage_StunHalfSecond_Event()
        {
            var s = Enemy();
            float dmg = s.WallImpact(200f, Src, 5, 0, Vector3.zero);
            Assert.AreEqual(20f, dmg, 1e-4f);
            Assert.AreEqual(1, _ev.Count<OnWallImpact>());
            Assert.AreEqual(20f, _ev.Read<OnWallImpact>()[0].Value, 1e-4f);
            Assert.IsTrue(s.IsStunned(5 + 29));
            Assert.IsFalse(s.IsStunned(5 + 30)); // 0.5 s
        }

        // ---------------------------------------------------------------- Slow

        [Test]
        public void Slow_StrongestWins_NoStacking_EnemyCap()
        {
            var s = Enemy();
            Assert.IsTrue(s.ApplySlow(0.3f, 0f, 2f, 0));
            Assert.AreEqual(0.7f, s.MoveSpeedMultiplier(1), 1e-5f);
            Assert.IsFalse(s.ApplySlow(0.2f, 0f, 5f, 1)); // weaker is ignored
            Assert.AreEqual(0.7f, s.MoveSpeedMultiplier(1), 1e-5f);
            Assert.IsTrue(s.ApplySlow(0.5f, 0.2f, 2f, 2));
            Assert.AreEqual(0.5f, s.MoveSpeedMultiplier(3), 1e-5f);
            Assert.AreEqual(0.8f, s.AttackSpeedMultiplier(3), 1e-5f);
            Assert.IsTrue(s.ApplySlow(0.9f, 0f, 2f, 4)); // clamped at -60%
            Assert.AreEqual(0.4f, s.MoveSpeedMultiplier(5), 1e-5f);
            Assert.AreEqual(1f, s.MoveSpeedMultiplier(4 + 120), 1e-5f); // expired
        }

        [Test]
        public void Slow_PlayerFloor_40PercentOfBaseSpeed()
        {
            var s = Player();
            s.ApplySlow(0.9f, 0.9f, 3f, 0);
            Assert.GreaterOrEqual(s.MoveSpeedMultiplier(1), 0.4f - 1e-5f);
            Assert.GreaterOrEqual(s.AttackSpeedMultiplier(1), 0.4f - 1e-5f);
        }

        // ---------------------------------------------------------------- Stealth

        [Test]
        public void Stealth_MaxDuration4s_BreaksOnDamage_Icd1s()
        {
            var s = Enemy();
            Assert.IsTrue(s.EnterStealth(10f, 0));
            Assert.IsFalse(s.IsTargetable(1));
            Assert.AreEqual(1, _ev.Count<OnStealthEnter>());
            s.Tick(239);
            Assert.IsTrue(s.IsStealthed(239));
            s.Tick(240); // 4 s cap: expires and publishes the break
            Assert.IsTrue(s.IsTargetable(240));
            Assert.AreEqual(1, _ev.Count<OnStealthBreak>());
            Assert.IsFalse(s.EnterStealth(2f, 299)); // ICD 1 s from the end
            Assert.IsTrue(s.EnterStealth(2f, 300));
            s.NotifyDamageDealt(310, true); // exempt effect: keeps Stealth
            Assert.IsTrue(s.IsStealthed(311));
            s.NotifyDamageTaken(320);
            Assert.IsTrue(s.IsTargetable(320));
            Assert.AreEqual(2, _ev.Count<OnStealthBreak>());
        }

        [Test]
        public void Stealth_BreaksOnDealingDamage_AndReveal3m()
        {
            var s = Enemy();
            s.EnterStealth(3f, 0);
            s.NotifyDamageDealt(5);
            Assert.IsTrue(s.IsTargetable(5));
            s.EnterStealth(3f, 100);
            Assert.IsFalse(s.RevealIfNear(3.1f, 101));
            Assert.IsTrue(s.RevealIfNear(3f, 101));
            Assert.IsTrue(s.IsTargetable(101));
        }

        // ---------------------------------------------------------------- Parry / Intangible

        [Test]
        public void Parry_Window12Frames_NegatesNormalHit_AttackerTakes50()
        {
            var s = Player();
            Assert.AreEqual(21, s.OpenParry(10));
            Assert.IsFalse(s.IsParryActive(9));
            Assert.IsTrue(s.IsParryActive(10));
            Assert.IsTrue(s.IsParryActive(21));
            Assert.IsFalse(s.IsParryActive(22));
            Assert.IsTrue(s.TryParry(HitFlags.None, 21, out float stagger));
            Assert.AreEqual(50f, stagger);
            Assert.IsFalse(s.TryParry(HitFlags.None, 22, out _));
        }

        [Test]
        public void Parry_BonusCappedAt03s_18Frames()
        {
            var s = Player();
            Assert.AreEqual(10 + 18 - 1, s.OpenParry(10, 0.1f)); // 12 + 6 = 18
            Assert.AreEqual(10 + 18 - 1, s.OpenParry(10, 5f));   // far above the cap
        }

        [Test]
        public void Parry_DoesNotWorkAgainst_UnblockableOrAnnihilation()
        {
            var s = Player();
            s.OpenParry(0);
            Assert.IsFalse(s.TryParry(HitFlags.Unblockable, 1, out _));
            Assert.IsFalse(s.TryParry(HitFlags.Annihilation, 1, out _));
        }

        [Test]
        public void Intangible_AddsInvulnerabilityWindow_AndPassThrough()
        {
            var s = Player();
            s.Invulnerability = new InvulnerabilityState();
            s.GrantIntangible(10, 19);
            Assert.IsTrue(s.Invulnerability.IsInvulnerable(10));
            Assert.IsTrue(s.Invulnerability.IsInvulnerable(19));
            Assert.IsFalse(s.Invulnerability.IsInvulnerable(20));
            Assert.IsTrue(s.IsIntangible(19));
            Assert.IsFalse(s.IsIntangible(20));
        }

        // ---------------------------------------------------------------- Player hit reaction

        [Test]
        public void PlayerReaction_Light_FlashOnly()
        {
            Assert.AreEqual(PlayerReaction.Flash, Player().BeginPlayerReaction(PlayerHitWeight.Light, false, 0));
        }

        [Test]
        public void PlayerReaction_Flinch8Frames_DodgeCancelAfter6()
        {
            var s = Player();
            Assert.AreEqual(PlayerReaction.Flinch, s.BeginPlayerReaction(PlayerHitWeight.Medium, false, 100));
            Assert.IsFalse(s.CanDodgeCancelReaction(105));
            Assert.IsTrue(s.CanDodgeCancelReaction(106));
            s.Tick(108);
            Assert.AreEqual(PlayerReaction.None, s.CurrentReaction);
        }

        [Test]
        public void PlayerReaction_Knockdown24Frames_IFramesLast10()
        {
            var s = Player();
            s.Invulnerability = new InvulnerabilityState();
            Assert.AreEqual(PlayerReaction.Knockdown, s.BeginPlayerReaction(PlayerHitWeight.Heavy, true, 100));
            Assert.IsFalse(s.Invulnerability.IsInvulnerable(113));
            Assert.IsTrue(s.Invulnerability.IsInvulnerable(114));
            Assert.IsTrue(s.Invulnerability.IsInvulnerable(123));
            Assert.IsFalse(s.Invulnerability.IsInvulnerable(124));
            Assert.IsFalse(s.CanDodgeCancelReaction(110));
            Assert.AreEqual(PlayerReaction.None, s.BeginPlayerReaction(PlayerHitWeight.Medium, false, 110)); // no re-trigger mid-fall
        }

        [Test]
        public void PlayerReaction_SuperArmor_IgnoresFlinch()
        {
            var s = Player();
            s.GrantUnstoppable(1000);
            Assert.AreEqual(PlayerReaction.None, s.BeginPlayerReaction(PlayerHitWeight.Medium, false, 0));
        }
    }
}

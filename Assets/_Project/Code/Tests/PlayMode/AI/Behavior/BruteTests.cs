#if UNITY_EDITOR
using System.Collections;
using Eclipse.AI.Enemies;
using Eclipse.Core.Stats;
using Eclipse.Core.Tags;
using Eclipse.Gameplay.Combat;
using Eclipse.Tests.PlayMode.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Eclipse.Tests.PlayMode.AI.Behavior
{
    /// <summary>M07 criterion 4 (09 section 4.4): Kiln Brute front plate -50%, back 100%; the slam is Unblockable but not Unavoidable.</summary>
    public class BruteTests
    {
        private const int DodgeWindowSource = 9100;
        private AiRun _run;

        [TearDown]
        public void TearDown() => _run?.Restore();

        [UnityTest]
        public IEnumerator Brute_takes_half_damage_from_the_front_and_full_damage_from_behind()
        {
            _run = new AiRun();
            yield return _run.Load();
            _run.PlacePlayer(Vector3.zero, Vector3.forward);
            EnemyController b = _run.Spawn(AiRun.BruteIndex, new Vector3(0f, 0f, 8f));
            yield return _run.Settle();
            Assert.IsTrue(b.IsReady);

            // No tick ran since the spawn: the brute still faces the player (SpawnAt turns it toward the target), i.e. -Z.
            Vector3 facing = b.Entity.Facing;
            Assert.Less(facing.z, -0.99f, "test setup: the brute must face the player");
            var receiver = b.Actor.Hurtbox.Receiver;
            const float raw = 20f;

            // Attacker in front of the brute (between it and the player): the hit travels along the facing's opposite, +Z.
            var front = receiver.ReceiveHit(CombatEnv.Hit(raw), Vector3.forward);
            // Attacker behind it: the hit travels along the brute's facing, -Z.
            var back = receiver.ReceiveHit(CombatEnv.Hit(raw), Vector3.back);

            Debug.Log("[M07 T10] brute raw " + raw + " -> front " + front.FinalDamage + ", back " + back.FinalDamage);
            Assert.AreEqual(raw * 0.5f, front.FinalDamage, 1e-3f, "front plate must cut 50% (front_plate_reduction_pct = 50)");
            Assert.AreEqual(raw, back.FinalDamage, 1e-3f, "from behind the brute takes 100%");
        }

        [UnityTest]
        public IEnumerator Slam_ignores_Block_but_is_negated_by_dodge_iframes()
        {
            _run = new AiRun();
            yield return _run.Load();
            _run.PlacePlayer(Vector3.zero, Vector3.forward);

            // Block at its cap (blockCap 0.5): without Unblockable roughly half the hits would be reduced by blockAmount.
            _run.Player.Entity.Stats.SetBase(StatId.BlockChance, 0.5f);
            CombatEntity player = _run.Player.Entity;

            // Control: a hit with no flags is Blocked often enough, so the setup really exercises Block.
            int blocked = 0;
            float slamRaw = 0f;
            EnemyController probe = _run.Spawn(AiRun.BruteIndex, new Vector3(0f, 0f, 3f));
            yield return _run.Settle();
            Assert.IsTrue(probe.IsReady);
            AttackPlan slamPlan = probe.Blueprint.Heads[0];
            slamRaw = slamPlan.Damage;
            Assert.IsTrue((slamPlan.Flags & HitFlags.Unblockable) != 0, "the slam must carry Unblockable (attack.enemy.brute.slam hit_flags)");
            Assert.IsTrue((slamPlan.Flags & HitFlags.Unavoidable) == 0, "the slam must not be Unavoidable: dodge i-frames must still work");

            // A stand-alone twin of the player (same Armor, attacker level, Block and incoming pipeline) so the probes cannot leave
            // grace-period state on the real player.
            var env = new CombatEnv();
            CombatEntity twin = env.MakeEntity(Team.Player, _run.World.Config.kaelMaxHealth);
            twin.Stats.SetBase(StatId.Armor, _run.World.Config.kaelArmor);
            twin.Stats.SetBase(StatId.BlockChance, 0.5f);
            twin.IncomingAttackerLevel = _run.World.Config.attackerLevel;
            int frame = 1000;
            for (int i = 0; i < 60; i++)
            {
                twin.CurrentFrame = frame += 100; // far apart: no grace period between the probes
                twin.Health.Restore(1f);
                if (twin.ReceiveHit(CombatEnv.Hit(slamRaw), Vector3.back).Result == HitResult.Blocked) blocked++;
            }
            Assert.GreaterOrEqual(blocked, 8, "control: Block must be active in this setup (got " + blocked + "/60)");

            // The reference value of an unblockable slam through the very same pipeline.
            twin.CurrentFrame = frame += 100;
            twin.Health.Restore(1f);
            float reference = twin.ReceiveHit(CombatEnv.Hit(slamRaw, HitFlags.Unblockable), Vector3.back).FinalDamage;
            env.Dispose();
            Assert.Greater(reference, 0f);

            // Real flow. Phase A: dodge i-frames cover the slam, nothing lands. Phase B: they end, the slam lands unreduced every time.
            player.Invulnerability.AddWindow(0, int.MaxValue - 1, 0, DodgeWindowSource);
            bool impact = false;
            for (int t = 0; t < 900 && !impact; t++)
            {
                _run.Tick();
                ExecutorPhase phase = probe.Binder.Executor.Phase;
                impact = phase == ExecutorPhase.Active || phase == ExecutorPhase.StepRecovery
                    || (phase == ExecutorPhase.Idle && probe.Binder.Executor.LastResult == ExecutorResult.Completed);
            }
            Assert.IsTrue(impact, "the brute never reached the impact of its slam");
            _run.Tick(30);
            Assert.AreEqual(0, _run.HitsOnPlayer.Count, "dodge i-frames must negate the slam");
            Assert.AreEqual(player.Health.Max, player.Health.Current, 1e-3f);

            player.Invulnerability.RemoveSource(DodgeWindowSource);
            int landed = 0;
            for (int t = 0; t < 3000 && landed < 4; t++)
            {
                int before = _run.HitsOnPlayer.Count;
                _run.Tick();
                for (int i = before; i < _run.HitsOnPlayer.Count; i++)
                {
                    OnHitDealt h = _run.HitsOnPlayer[i];
                    landed++;
                    Assert.AreEqual(probe.Entity.Id, h.Source);
                    Assert.IsTrue((h.Flags & HitFlags.Unblockable) != 0, "the landed hit lost its Unblockable flag");
                    Assert.AreEqual(reference, h.Value, 1e-3f, "slam " + landed + " was reduced: Block must not apply to Unblockable");
                }
                player.Health.Restore(1f);
            }
            Assert.GreaterOrEqual(landed, 4, "expected four slams to land once the i-frames ended");
        }
    }
}
#endif

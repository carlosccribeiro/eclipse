#if UNITY_EDITOR
using System.Collections.Generic;
using Eclipse.Data.Combat;
using Eclipse.Gameplay.Combat.Attacks;
using Eclipse.Gameplay.Input;
using NUnit.Framework;

namespace Eclipse.Tests.PlayMode.Combat.Attacks
{
    /// <summary>06 3.5 / 3.6: Greatsword frame data per phase at Attack Speed 1.0 and 1.5, driven through the real sequencer.</summary>
    public class AttackFrameDataTests
    {
        private static int[] Run(AttackStep step, bool heavy, float speed)
        {
            var set = new AttackSetData { Weight = WeaponWeightClass.Heavy, MaxAttackSpeed = 2f };
            if (heavy) { set.HasHeavy = true; set.Heavy = step; } else set.Basic = new[] { step };
            var seq = new AttackSequencer(set);
            var buf = new InputBuffer();
            buf.Push(heavy ? InputActionId.Heavy : InputActionId.Basic, 1);
            var counts = new int[4];
            for (int i = 0; i < 400; i++)
            {
                seq.Tick(1 + i, buf, false, false, speed, AttackExternalState.Free);
                AttackPhase p = seq.State.Phase;
                if (p == AttackPhase.None) { if (i > 0) break; continue; }
                counts[(int)p]++;
            }
            return counts;
        }

        private static void Check(int[] c, int s, int a, int r, string label)
        {
            Assert.AreEqual(s, c[(int)AttackPhase.Startup], label + " Startup");
            Assert.AreEqual(a, c[(int)AttackPhase.Active], label + " Active");
            Assert.AreEqual(r, c[(int)AttackPhase.Recovery], label + " Recovery");
        }

        [Test]
        public void Greatsword_phases_at_attack_speed_1_0()
        {
            var w = CombatEnv.LoadWeapon();
            Check(Run(w.BasicChain[0].Step, false, 1f), 20, 6, 40, "hit1");
            Check(Run(w.BasicChain[1].Step, false, 1f), 20, 6, 40, "hit2");
            Check(Run(w.BasicChain[2].Step, false, 1f), 26, 8, 34, "hit3");
            Check(Run(w.Heavy.Step, true, 1f), 36, 6, 26, "heavy (released at min charge)");
        }

        [Test]
        public void Greatsword_phases_scale_with_attack_speed_1_5()
        {
            var w = CombatEnv.LoadWeapon();
            // ceil(frames / 1.5) per phase (06 3.5: the three phases scale equally; hit-stop does not).
            Check(Run(w.BasicChain[0].Step, false, 1.5f), 14, 4, 27, "hit1");
            Check(Run(w.BasicChain[1].Step, false, 1.5f), 14, 4, 27, "hit2");
            Check(Run(w.BasicChain[2].Step, false, 1.5f), 18, 6, 23, "hit3");
            Check(Run(w.Heavy.Step, true, 1.5f), 24, 4, 18, "heavy");
        }

        [Test]
        public void Attack_speed_is_capped_at_the_weapon_cap()
        {
            var w = CombatEnv.LoadWeapon();
            Check(Run(w.BasicChain[0].Step, false, 5f), 10, 3, 20, "hit1 at cap 2.0");
        }

        [Test]
        public void Combo_chain_advances_0_1_2_then_wraps_and_chains_back_to_back()
        {
            var w = CombatEnv.LoadWeapon();
            var seq = new AttackSequencer(AttackController.BuildSet(w, 2f, false));
            var buf = new InputBuffer();
            var starts = new List<int>();
            var idx = new List<int>();
            int pushed = 0;
            buf.Push(InputActionId.Basic, 1);
            for (int f = 1; f < 300; f++)
            {
                if (seq.State.Phase == AttackPhase.Recovery && seq.State.PhaseFrame == 1 && pushed < 3) { buf.Push(InputActionId.Basic, f); pushed++; }
                seq.Tick(f, buf, false, false, 1f, AttackExternalState.Free);
                if (seq.StartedThisTick) { starts.Add(f); idx.Add(seq.CurrentComboIndex); }
            }
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 0 }, idx);
            // 66 frames (20+6+40), 66, 68 (26+8+34): each next hit starts the tick after the previous Recovery ends.
            CollectionAssert.AreEqual(new[] { 1, 67, 133, 201 }, starts);
        }
    }
}
#endif

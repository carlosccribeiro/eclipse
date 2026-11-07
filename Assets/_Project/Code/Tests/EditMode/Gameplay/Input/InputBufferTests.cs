using Eclipse.Gameplay.Input;
using Eclipse.Gameplay.Simulation;
using NUnit.Framework;

namespace Eclipse.Tests.EditMode.Gameplay.Input
{
    public sealed class InputBufferTests
    {
        private static void Step(PlayerInputReader r, ref int frame, int n = 1)
        {
            for (int i = 0; i < n; i++) r.Tick(new SimulationFrame(++frame));
        }

        [Test]
        public void Entry_IsConsumableUntilFrame8_AndExpiresAtFrame9()
        {
            var b = new InputBuffer();
            b.Push(InputActionId.Dodge, 10);
            Assert.IsTrue(b.Contains(InputActionId.Dodge, 18));
            Assert.IsFalse(b.Contains(InputActionId.Dodge, 19));
            Assert.IsFalse(b.TryConsume(19, out _));

            b.Push(InputActionId.Dodge, 30);
            Assert.IsTrue(b.TryConsume(38, out var a));
            Assert.AreEqual(InputActionId.Dodge, a.Id);
            Assert.AreEqual(0, b.Count);
        }

        [Test]
        public void Priority_DodgeOverAbilityOverHeavyOverBasic()
        {
            var b = new InputBuffer();
            b.Push(InputActionId.Dodge, 1);
            b.Push(InputActionId.Skill1, 2);
            b.Push(InputActionId.Heavy, 3);
            b.Push(InputActionId.Basic, 4);
            var order = new[] { InputActionId.Dodge, InputActionId.Skill1, InputActionId.Heavy, InputActionId.Basic };
            foreach (var expected in order)
            {
                Assert.IsTrue(b.TryConsume(5, out var a));
                Assert.AreEqual(expected, a.Id);
            }
            Assert.IsFalse(b.TryConsume(5, out _));
        }

        [Test]
        public void SamePriority_LastPressWins_EvenOnSameFrame()
        {
            var b = new InputBuffer();
            b.Push(InputActionId.Skill1, 5);
            b.Push(InputActionId.Skill3, 7);
            b.Push(InputActionId.Skill2, 7);
            Assert.IsTrue(b.TryConsume(8, out var a));
            Assert.AreEqual(InputActionId.Skill2, a.Id);
            Assert.IsTrue(b.TryConsume(8, out a));
            Assert.AreEqual(InputActionId.Skill3, a.Id);
        }

        [Test]
        public void AcceptedMask_FiltersWithoutConsumingOthers()
        {
            var b = new InputBuffer();
            b.Push(InputActionId.Dodge, 1);
            b.Push(InputActionId.Basic, 2);
            Assert.IsTrue(b.TryConsume(3, InputActionId.Basic.Bit(), out var a));
            Assert.AreEqual(InputActionId.Basic, a.Id);
            Assert.AreEqual(1, b.Count);
        }

        [Test]
        public void HoldingBasic_RepeatsAfterEachConsume()
        {
            var r = new PlayerInputReader();
            int f = 0;
            r.Press(InputActionId.Basic);
            Step(r, ref f);
            Assert.IsTrue(r.Buffer.TryConsume(f, out var a));
            Assert.AreEqual(InputActionId.Basic, a.Id);
            Assert.IsFalse(r.Buffer.Contains(InputActionId.Basic, f));
            Step(r, ref f);
            Assert.IsTrue(r.Buffer.Contains(InputActionId.Basic, f), "held LMB must re-buffer");
            Assert.AreEqual(1, r.Buffer.Count, "repeat keeps a single entry");

            r.Release(InputActionId.Basic);
            r.Buffer.TryConsume(f, out _);
            Step(r, ref f, 3);
            Assert.AreEqual(0, r.Buffer.Count);
        }

        [Test]
        public void GamepadChord_LbPlusRb_EmitsOnlyUltimate()
        {
            var r = new PlayerInputReader();
            int f = 0;
            r.Press(InputActionId.Skill1, true);
            Step(r, ref f);
            r.Press(InputActionId.Skill2, true);
            Step(r, ref f);
            Step(r, ref f, PlayerInputReader.ChordWindowFrames + 1);
            Assert.IsTrue(r.Buffer.TryConsume(f, out var a));
            Assert.AreEqual(InputActionId.Ultimate, a.Id);
            Assert.IsFalse(r.Buffer.TryConsume(f, out _), "no isolated LB/RB");
        }

        [Test]
        public void GamepadChord_SameFrame_EmitsOnlyUltimate()
        {
            var r = new PlayerInputReader();
            int f = 0;
            r.Press(InputActionId.Skill2, true);
            r.Press(InputActionId.Skill1, true);
            Step(r, ref f, 1 + PlayerInputReader.ChordWindowFrames);
            Assert.IsTrue(r.Buffer.TryConsume(f, out var a));
            Assert.AreEqual(InputActionId.Ultimate, a.Id);
            Assert.AreEqual(0, r.Buffer.Count);
        }

        [Test]
        public void GamepadShoulder_Alone_EmitsSkillAfterWindowWithOriginalStamp()
        {
            var r = new PlayerInputReader();
            int f = 0;
            r.Press(InputActionId.Skill1, true);
            Step(r, ref f);
            Assert.AreEqual(0, r.Buffer.Count, "held back during chord window");
            Step(r, ref f, PlayerInputReader.ChordWindowFrames);
            Assert.IsTrue(r.Buffer.TryConsume(f, out var a));
            Assert.AreEqual(InputActionId.Skill1, a.Id);
            Assert.AreEqual(1, a.Frame);
        }

        [Test]
        public void GamepadShoulders_FarApart_AreTwoSkills()
        {
            var r = new PlayerInputReader();
            int f = 0;
            r.Press(InputActionId.Skill1, true);
            Step(r, ref f, PlayerInputReader.ChordWindowFrames + 1);
            r.Press(InputActionId.Skill2, true);
            Step(r, ref f, PlayerInputReader.ChordWindowFrames + 1);
            Assert.AreEqual(2, r.Buffer.Count);
            Assert.IsTrue(r.Buffer.TryConsume(f, out var a));
            Assert.AreEqual(InputActionId.Skill2, a.Id);
        }

        [Test]
        public void Keyboard_QAndE_AreNotChordedOrDelayed()
        {
            var r = new PlayerInputReader();
            int f = 0;
            r.Press(InputActionId.Skill1);
            r.Press(InputActionId.Skill2);
            Step(r, ref f);
            Assert.AreEqual(2, r.Buffer.Count);
        }

        [Test]
        public void Ultimate_KeyboardF_IsBuffered()
        {
            var r = new PlayerInputReader();
            int f = 0;
            r.Press(InputActionId.Ultimate);
            Step(r, ref f);
            Assert.IsTrue(r.Buffer.TryConsume(f, out var a));
            Assert.AreEqual(InputActionId.Ultimate, a.Id);
        }
    }
}

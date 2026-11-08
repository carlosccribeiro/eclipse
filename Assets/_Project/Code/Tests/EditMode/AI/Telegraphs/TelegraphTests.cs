using Eclipse.AI.Telegraphs;
using Eclipse.Data.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Tests.EditMode.AI.Telegraphs
{
    public class TelegraphTests
    {
        private const float Tick = 1f / 60f;

        [Test]
        public void Timeline_ImpactsOnTheTickProgressReachesOne()
        {
            var t = new TelegraphTimeline();
            t.Start(0.6f, false); // 36 ticks at 60 Hz
            int impactTick = -1;
            for (int i = 1; i <= 60; i++)
            {
                TelegraphEvent e = t.Advance(Tick);
                if (e == TelegraphEvent.Impact) { impactTick = i; break; }
                Assert.Less(t.Progress, 1f, "progress must stay below 1 before impact (tick " + i + ")");
            }
            Assert.AreEqual(36, impactTick);
            Assert.AreEqual(1f, t.Progress, 1e-6f);
            Assert.AreEqual(TelegraphEvent.None, t.Advance(Tick), "impact fires once");
        }

        [Test]
        public void Timeline_ProgressIsLinear()
        {
            var t = new TelegraphTimeline();
            t.Start(1.0f, false);
            t.Advance(0.25f);
            Assert.AreEqual(0.25f, t.Progress, 1e-6f);
        }

        [Test]
        public void Timeline_InterruptibleIsCancelledByStagger_NonInterruptibleIsNot()
        {
            var t = new TelegraphTimeline();
            t.Start(0.6f, true);
            t.Advance(0.2f);
            Assert.IsTrue(t.TryInterrupt());
            Assert.IsFalse(t.IsRunning);
            Assert.AreEqual(TelegraphEvent.None, t.Advance(1f), "no impact after cancel");

            t.Start(0.6f, false);
            Assert.IsFalse(t.TryInterrupt());
            Assert.IsTrue(t.IsRunning);
        }

        [Test]
        public void WindupMath_ReducesButCapsAt15PercentAndRespectsFloor()
        {
            // 06 section 3.10: common 0.6 s Normal, -15% -> 0.51 s, floor 0.45.
            Assert.AreEqual(0.51f, WindupMath.Effective(0.6f, 0.15f, WindupClass.Common), 1e-5f);
            Assert.AreEqual(0.51f, WindupMath.Effective(0.6f, 0.50f, WindupClass.Common), 1e-5f, "reduction capped at 15%");
            Assert.AreEqual(0.45f, WindupMath.Effective(0.4f, 0f, WindupClass.Common), 1e-5f, "clamped to floor");
            Assert.AreEqual(0.55f, WindupMath.Effective(0.5f, 0f, WindupClass.Elite), 1e-5f);
            Assert.AreEqual(0.8f, WindupMath.Effective(0.7f, 0f, WindupClass.Boss), 1e-5f);
            Assert.AreEqual(1.5f, WindupMath.Effective(1.0f, 0f, WindupClass.Boss, annihilation: true), 1e-5f);
            Assert.AreEqual(0.8f, WindupMath.Effective(0.6f, 0f, WindupClass.Common, sniper: true), 1e-5f);
        }

        [Test]
        public void Palette_HasNoOrange_AndCoversEveryVisual()
        {
            var palette = AssetDatabase.LoadAssetAtPath<TelegraphPalette>(Eclipse.Editor.Setup.TelegraphPaletteCreator.AssetPath);
            Assert.IsNotNull(palette, "run Eclipse.Editor.Setup.TelegraphPaletteCreator.Run");
            for (int i = 0; i < TelegraphVisuals.Count; i++)
            {
                var v = (TelegraphVisual)i;
                Assert.IsTrue(palette.TryGet(v, out TelegraphPalette.Entry e), "missing entry " + v);
                Assert.IsFalse(TelegraphPalette.IsOrange(e.fill), v + " fill is orange");
                Assert.IsFalse(TelegraphPalette.IsOrange(e.border), v + " border is orange");
            }
        }

        [Test]
        public void IsOrange_DetectsOrangeButNotRedOrYellow()
        {
            Assert.IsTrue(TelegraphPalette.IsOrange(new Color(1f, 0.5f, 0f)));
            Assert.IsFalse(TelegraphPalette.IsOrange(new Color(0.85f, 0.1f, 0.1f)));
            Assert.IsFalse(TelegraphPalette.IsOrange(new Color(1f, 0.92f, 0.1f)));
        }

        [Test]
        public void ViewPool_RentsFromFixedCapacity()
        {
            Shader shader = Shader.Find("Eclipse/TelegraphFill");
            Assert.IsNotNull(shader);
            var pool = new TelegraphViewPool(shader, 2);
            try
            {
                TelegraphView a = pool.Rent();
                TelegraphView b = pool.Rent();
                Assert.IsNotNull(a);
                Assert.IsNotNull(b);
                Assert.IsNull(pool.Rent(), "exhausted pool returns null, never instantiates");
                pool.Return(a);
                Assert.AreSame(a, pool.Rent());
            }
            finally { pool.Destroy(); }
        }
    }
}

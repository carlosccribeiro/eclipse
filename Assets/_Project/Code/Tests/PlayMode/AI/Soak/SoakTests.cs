#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using Eclipse.AI.Diagnostics;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Eclipse.Tests.PlayMode.AI.Soak
{
    /// <summary>
    /// M07 T9 (mode a): short soak in the Editor. Checks the 200 live enemies and the GC tail against the idle baseline of the same
    /// run (the Editor adds noise of its own, see CLAUDE.md section 9). The wall-clock budget (frame, gameplay+AI, physics) is measured
    /// by the development build on the target hardware, not asserted here.
    /// </summary>
    public class SoakTests
    {
        private const float SettleSeconds = 3f;
        private const float MeasureSeconds = 10f;

        [Test]
        public void Percentile_is_nearest_rank()
        {
            var sorted = new float[100];
            for (int i = 0; i < 100; i++) sorted[i] = i + 1;
            Assert.AreEqual(95.0, PerfSampler.Percentile(sorted, 0.95));
            Assert.AreEqual(99.0, PerfSampler.Percentile(sorted, 0.99));
            Assert.AreEqual(100.0, PerfSampler.Percentile(new[] { 1f, 100f }, 0.99));
        }

        private static float Median(float[] a)
        {
            var c = (float[])a.Clone();
            Array.Sort(c);
            return c.Length == 0 ? 0f : c[c.Length / 2];
        }

        private static float Percentile(float[] a, double p)
        {
            var c = (float[])a.Clone();
            Array.Sort(c);
            return (float)PerfSampler.Percentile(c, p);
        }

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator Two_hundred_enemies_stay_alive_and_the_gc_tail_matches_idle()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Scenes/Perf/SoakTest.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;

            var runner = UnityEngine.Object.FindFirstObjectByType<SoakTestRunner>();
            Assert.IsNotNull(runner, "SoakTestRunner missing from the scene");
            string csv = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "soak_editor.csv"));
            runner.Configure(SettleSeconds, MeasureSeconds, false, csv, 240);
            runner.Begin();

            float deadline = Time.realtimeSinceStartup + 240f;
            while (!runner.IsFinished && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(runner.IsFinished, "soak did not finish; phase " + runner.Current + ", alive " + runner.Alive);

            PerfSummary r = runner.Result;
            Debug.Log("[SoakTest] " + SoakTestRunner.HardwareDescription + "\n[SoakTest] " + r);
            Assert.IsTrue(File.Exists(csv), "CSV not written");
            Assert.IsTrue(r.GameplayValid, "gameplay profiler recorder is not valid");
            Assert.Greater(r.GameplayMs.Mean, 0.0, "no gameplay samples recorded");
            Assert.Greater(r.AiTickMs.Mean, 0.0, "AI tick marker recorded nothing");
            Assert.GreaterOrEqual(r.PeakAlive, 200, "never reached 200 live enemies");
            Assert.GreaterOrEqual(r.MinAlive, 180, "live count dropped too far below 200 during the window");
            Assert.GreaterOrEqual(r.Frames, 60);

            float[] baseline = runner.BaselineGc();
            float[] loop = runner.MeasuredGc();
            Debug.Log("[SoakTest] GC bytes/frame: idle median " + Median(baseline) + " p95 " + Percentile(baseline, 0.95)
                + " max " + Percentile(baseline, 1.0) + " | loop median " + Median(loop) + " p95 " + Percentile(loop, 0.95) + " max " + Percentile(loop, 1.0));
            Assert.LessOrEqual(Median(loop), Median(baseline), "typical soak frame allocates more than an idle frame");
            Assert.LessOrEqual(Percentile(loop, 0.95), Math.Max(Percentile(baseline, 0.95), Percentile(baseline, 1.0)) * 2 + 1024,
                "soak GC tail above the idle tail");
        }
    }
}
#endif

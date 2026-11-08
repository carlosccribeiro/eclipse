#if UNITY_EDITOR
using System.Collections;
using Eclipse.Gameplay.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Eclipse.Tests.PlayMode.Combat.Feedback
{
    /// <summary>60 s of continuous Greatsword combo (3.600 ticks) against a dummy with 0 B of managed allocation per frame.</summary>
    public class ComboGcTests
    {
        private SandboxCombatRun _run;
        private int _savedVSync, _savedFps;

        [TearDown]
        public void TearDown()
        {
            _run?.Restore();
            QualitySettings.vSyncCount = _savedVSync;
            Application.targetFrameRate = _savedFps;
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator Sixty_second_combo_allocates_nothing_per_frame()
        {
            _savedVSync = QualitySettings.vSyncCount;
            _savedFps = Application.targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;

            yield return SandboxKit.Load();
            _run = new SandboxCombatRun();
            _run.Setup("TrainingDummy_Medium", noCrit: false);
            _run.Press(InputActionId.Basic); // held for the whole run

            var result = new GcResult();
            var hitFrame = new bool[3600];
            int n = 0;
            yield return GcProbe.Measure(result, () =>
            {
                int before = _run.PlayerHits.Count;
                _run.Tick();
                if (n < hitFrame.Length) hitFrame[n++] = _run.PlayerHits.Count > before;
            }, 600, 3000);
            // Sample i of the loop belongs to perFrame call 600 + i. Report which spikes coincide with a hit landing.
            int spikesOnHit = 0, spikesElsewhere = 0, hitFrames = 0;
            for (int i = 0; i < result.Loop.Length; i++)
            {
                bool hit = hitFrame[600 + i];
                if (hit) hitFrames++;
                if (result.Loop[i] > result.BaselineMedianBytes) { if (hit) spikesOnHit++; else spikesElsewhere++; }
            }
            Debug.Log("[ComboGc] hit frames " + hitFrames + ", spikes on hit frames " + spikesOnHit + ", elsewhere " + spikesElsewhere);

            _run.Release(InputActionId.Basic);
            Assert.GreaterOrEqual(_run.PlayerHits.Count, 20, "the combo kept landing hits");
            GcProbe.AssertClean(result);
        }
    }
}
#endif

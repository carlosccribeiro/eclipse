#if UNITY_EDITOR
using System.Collections;
using Eclipse.AI.Enemies;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Eclipse.Tests.PlayMode.AI.Behavior
{
    /// <summary>M07 criterion 3 (09 section 4.2, 06 section 3.10): the Slag Hurler backs off when the player comes closer than 5 m and never throws off-screen.</summary>
    public class HurlerTests
    {
        private AiRun _run;
        private GameObject _farCamera;

        [TearDown]
        public void TearDown()
        {
            _run?.Restore();
            if (_farCamera != null) Object.Destroy(_farCamera);
        }

        [UnityTest]
        public IEnumerator Hurler_retreats_when_the_player_is_closer_than_5_m()
        {
            _run = new AiRun();
            yield return _run.Load();
            _run.PlacePlayer(Vector3.zero, Vector3.forward);
            _run.MakePlayerUntouchable();
            EnemyController h = _run.Spawn(AiRun.HurlerIndex, new Vector3(0f, 0f, 4f));
            yield return _run.Settle();
            Assert.IsTrue(h.IsReady);
            Assert.IsTrue(h.Binder.Retreat.Enabled, "retreat_below_m comes from data and must be set for the Hurler");

            float start = _run.PlanarDistance(h);
            Assert.Less(start, 5f);
            bool retreated = false;
            float maxDist = start;
            bool throwWhileClose = false;
            for (int t = 0; t < 180; t++) // 3 s
            {
                _run.Tick();
                retreated |= h.Binder.Retreat.Active;
                float d = _run.PlanarDistance(h);
                maxDist = Mathf.Max(maxDist, d);
                if (d < 5f && h.Binder.Executor.IsBusy) throwWhileClose = true;
            }
            Debug.Log("[M07 T10] hurler retreat: start " + start.ToString("F2") + " m, max " + maxDist.ToString("F2") + " m, end "
                + _run.PlanarDistance(h).ToString("F2") + " m");
            Assert.IsTrue(retreated, "RetreatPlanner never became active");
            Assert.Greater(maxDist, start + 2f, "the hurler must have moved away");
            Assert.GreaterOrEqual(maxDist, 5f, "it must back off to at least the 5 m trigger distance");
            Assert.IsFalse(throwWhileClose, "the Slag Throw has a minimum distance of 5 m");
        }

        [UnityTest]
        public IEnumerator Hurler_does_not_throw_off_screen_but_does_once_on_screen()
        {
            _run = new AiRun();
            yield return _run.Load();
            _run.PlacePlayer(Vector3.zero, Vector3.forward);
            _run.MakePlayerUntouchable();

            // A camera looking away from the arena: every enemy is behind it, so the 06 3.10 off-screen rule applies.
            _farCamera = new GameObject("FarCamera");
            _farCamera.transform.position = new Vector3(4000f, 10f, 4000f);
            var far = _farCamera.AddComponent<UnityEngine.Camera>();
            far.enabled = false;
            _run.Enemy.Camera = far;

            EnemyController h = _run.Spawn(AiRun.HurlerIndex, new Vector3(0f, 0f, 10f));
            yield return _run.Settle();
            Assert.IsTrue(h.IsReady);
            Assert.IsFalse(_run.Enemy.IsOnScreen(h.transform.position, _run.Frame), "test setup: the hurler must be off-screen");

            int busyTicks = 0;
            for (int t = 0; t < 1200; t++) // 20 s, several cooldowns
            {
                _run.Tick();
                if (h.Binder.Executor.IsBusy) busyTicks++;
            }
            Assert.AreEqual(0, busyTicks, "the hurler started an attack while off-screen");
            Assert.AreEqual(0, _run.HitsOnPlayer.Count);

            // Control: same enemy, same distance, but on-screen (no camera counts everything as visible).
            _run.Enemy.Camera = null;
            bool threw = false;
            for (int t = 0; t < 900 && !threw; t++)
            {
                _run.Tick();
                threw = h.Binder.Executor.IsBusy;
            }
            Assert.IsTrue(threw, "control: on-screen the hurler must attack, otherwise the off-screen check proves nothing");
        }
    }
}
#endif

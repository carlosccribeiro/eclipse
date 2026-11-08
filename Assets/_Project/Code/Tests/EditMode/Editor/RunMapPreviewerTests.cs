using Eclipse.Core.RunGen.Map;
using Eclipse.Editor.RunMapPreviewer;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Eclipse.Tests.EditMode.EditorTools
{
    [TestFixture]
    public sealed class RunMapPreviewerTests
    {
        private RunMapPreviewerWindow _window;

        [SetUp]
        public void SetUp()
        {
            // -nographics (CLAUDE.md section 6) has no graphics device: docking a real window logs an error. The
            // generation API does not need the UI, so fall back to a detached instance there.
            _window = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null
                ? ScriptableObject.CreateInstance<RunMapPreviewerWindow>()
                : EditorWindow.GetWindow<RunMapPreviewerWindow>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_window == null) return;
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) Object.DestroyImmediate(_window);
            else _window.Close();
        }

        [Test]
        public void Generate_seed_1_and_batch_100_have_no_violations()
        {
            RunMap map = _window.Generate(1UL);
            Assert.IsNotNull(map);
            Assert.AreEqual(14, map.Rows.Length);
            Assert.IsNotNull(_window.MinElitePath);
            Assert.IsNotNull(_window.MaxElitePath);
            Assert.LessOrEqual(MapPathAnalysis.CountElites(map, _window.MinElitePath), 2);
            Assert.GreaterOrEqual(MapPathAnalysis.CountElites(map, _window.MaxElitePath), 3);

            RunMapBatchResult r = _window.GenerateBatch(100);
            Assert.AreEqual(100, r.Count);
            Assert.AreEqual(0, r.Violations, r.FirstViolation);
            Assert.GreaterOrEqual(r.MeanAttempts, 1.0);
            Assert.GreaterOrEqual(r.TotalRooms, 100 * 14);
            Assert.Greater(r.RoomCounts[(int)RoomType.Combat], 0);
        }

        [Test]
        public void Plan_for_combat_node_is_within_budget()
        {
            RunMap map = _window.Generate(1UL);
            MapNode combat = map.Rows[0][0];
            Assert.AreEqual(RoomType.Combat, combat.Type);
            var plan = _window.PlanFor(combat);
            Assert.IsNull(plan.Error);
            Assert.LessOrEqual(plan.TotalThreat, plan.SpendableBudget);
        }

        [Test]
        public void Batch_of_1000_seeds_runs_under_10_seconds_with_no_violations()
        {
            RunMapBatchResult r = _window.GenerateBatch(1000);
            Debug.Log("[RunMapPreviewer] GenerateBatch(1000): " + r.ElapsedMs.ToString("0") + " ms, mean attempts "
                      + r.MeanAttempts.ToString("0.000") + ", fallback " + r.FallbackCount + ", row rebuilds mean " + r.MeanRowRebuilds.ToString("0.00"));
            Assert.AreEqual(0, r.Violations, r.FirstViolation);
            Assert.Less(r.ElapsedMs, 10000.0);
        }
    }
}

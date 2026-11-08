using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Eclipse.Core.Loot;
using Eclipse.Core.RunGen.Encounters;
using Eclipse.Core.RunGen.Map;
using Eclipse.Data.Definitions;
using Eclipse.Data.Run;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eclipse.Editor.RunMapPreviewer
{
    /// <summary>Aggregate of <see cref="RunMapPreviewerWindow.GenerateBatch"/> (M15 T6).</summary>
    public sealed class RunMapBatchResult
    {
        public int Count;
        public ulong FirstSeed;
        /// <summary>Rooms per <see cref="RoomType"/> (index = enum value), summed over all maps.</summary>
        public int[] RoomCounts = new int[Enum.GetValues(typeof(RoomType)).Length];
        /// <summary>Maps whose <see cref="MapConstraints.Validate"/> list is not empty. Must be 0.</summary>
        public int Violations;
        public string FirstViolation;
        public double MeanAttempts;
        public int MaxAttempts;
        public double MeanRowRebuilds;
        /// <summary>Maps that needed the deterministic template.</summary>
        public int FallbackCount;
        public double ElapsedMs;

        public int TotalRooms
        {
            get { int t = 0; for (int i = 0; i < RoomCounts.Length; i++) t += RoomCounts[i]; return t; }
        }
    }

    /// <summary>
    /// Editor window that generates and draws the run map by seed and reports statistics over N seeds
    /// (M15 T6, 11 §3.15 item 6). All generation goes through <see cref="MapGenerator"/> / <see cref="EncounterPlanner"/>;
    /// the window only presents results. Configs come from the T5 definitions in the registry when present, else Core defaults.
    /// </summary>
    public sealed class RunMapPreviewerWindow : EditorWindow
    {
        private const string UxmlPath = "Assets/_Project/Code/Editor/RunMapPreviewer/RunMapPreviewerWindow.uxml";
        private const string UssPath = "Assets/_Project/Code/Editor/RunMapPreviewer/RunMapPreviewerWindow.uss";

        private const float NodeW = 100f, NodeH = 40f, ColStep = 120f, RowStep = 66f, GraphW = 760f, TopPad = 14f;

        private static readonly Color PathMinColor = new Color(0.24f, 0.86f, 0.35f);
        private static readonly Color PathMaxColor = new Color(0.94f, 0.27f, 0.27f);
        private static readonly Color EdgeColor = new Color(0.55f, 0.55f, 0.55f);

        // Public inputs (also settable by tests; the UI fields write here).
        public ulong Seed { get; set; } = 1UL;
        public string RegionId { get; set; } = "ashen_wastes";
        public Difficulty Difficulty { get; set; } = Difficulty.Normal;
        public int AscensionLevel { get; set; }

        public RunMap CurrentMap { get; private set; }
        public int[] MinElitePath { get; private set; }
        public int[] MaxElitePath { get; private set; }
        public string ConfigSource { get; private set; } = "not loaded";

        private MapGenConfig _mapConfig;
        private EncounterConfig _encounterConfig;

        private VisualElement _graph;
        private Label _summary, _plan, _batch, _configLabel;
        private readonly List<Vector2[]> _edges = new List<Vector2[]>();
        private readonly List<Button> _nodeButtons = new List<Button>();
        private Button _selected;

        [MenuItem("Eclipse/Run Map Previewer")]
        public static void Open()
        {
            var w = GetWindow<RunMapPreviewerWindow>();
            w.titleContent = new GUIContent("Run Map Previewer");
            w.minSize = new Vector2(900f, 560f);
            w.Show();
        }

        // ------------------------------------------------------------------ UI

        public void CreateGUI()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
            if (uxml == null)
            {
                rootVisualElement.Add(new Label("Missing " + UxmlPath));
                return;
            }
            uxml.CloneTree(rootVisualElement);
            if (uss != null) rootVisualElement.styleSheets.Add(uss);

            var root = rootVisualElement;
            _graph = root.Q<VisualElement>("graph");
            _summary = root.Q<Label>("summary-label");
            _plan = root.Q<Label>("plan-label");
            _batch = root.Q<Label>("batch-label");
            _configLabel = root.Q<Label>("config-label");

            var seedField = root.Q<UnsignedLongField>("seed-field");
            seedField.SetValueWithoutNotify(Seed);
            seedField.RegisterValueChangedCallback(e => Seed = e.newValue);

            var regionField = root.Q<TextField>("region-field");
            regionField.SetValueWithoutNotify(RegionId);
            regionField.RegisterValueChangedCallback(e => RegionId = e.newValue);

            var diffField = root.Q<DropdownField>("difficulty-field");
            diffField.choices = new List<string>(Enum.GetNames(typeof(Difficulty)));
            diffField.SetValueWithoutNotify(Difficulty.ToString());
            diffField.RegisterValueChangedCallback(e => Difficulty = (Difficulty)Enum.Parse(typeof(Difficulty), e.newValue));

            var ascField = root.Q<IntegerField>("ascension-field");
            ascField.SetValueWithoutNotify(AscensionLevel);
            ascField.RegisterValueChangedCallback(e => AscensionLevel = Mathf.Clamp(e.newValue, 0, 100));

            root.Q<Button>("generate-button").clicked += () => { Generate(Seed); };
            var batchCount = root.Q<IntegerField>("batch-count");
            root.Q<Button>("batch-button").clicked += () =>
            {
                RunMapBatchResult r = GenerateBatch(Mathf.Max(1, batchCount.value));
                _batch.text = FormatBatch(r);
            };

            BuildLegend(root.Q<VisualElement>("legend"));
            _graph.generateVisualContent += DrawEdges;

            EnsureConfigs();
            _configLabel.text = "Config: " + ConfigSource;
            Generate(Seed);
        }

        private static void BuildLegend(VisualElement legend)
        {
            foreach (RoomType t in Enum.GetValues(typeof(RoomType)))
            {
                var item = new VisualElement();
                item.AddToClassList("rmp-legend-item");
                var sw = new VisualElement();
                sw.AddToClassList("rmp-legend-swatch");
                sw.AddToClassList(RoomClass(t));
                item.Add(sw);
                item.Add(new Label(t.ToString()));
                legend.Add(item);
            }
            var paths = new Label("green edge = path with <= 2 Elites, red edge = path with >= 3 Elites");
            paths.AddToClassList("rmp-note");
            legend.Add(paths);
        }

        // ------------------------------------------------------------------ generation API

        /// <summary>Generates the map of <paramref name="seed"/>, updates the drawing (when the UI exists) and returns it.</summary>
        public RunMap Generate(ulong seed)
        {
            EnsureConfigs();
            Seed = seed;
            CurrentMap = MapGenerator.Generate(seed, NewContext());
            MinElitePath = MapPathAnalysis.FindEliteExtremePath(CurrentMap, false);
            MaxElitePath = MapPathAnalysis.FindEliteExtremePath(CurrentMap, true);
            Redraw();
            return CurrentMap;
        }

        /// <summary>
        /// Generates <paramref name="n"/> consecutive seeds starting at <see cref="Seed"/> and aggregates the statistics.
        /// Does not touch the drawn map.
        /// </summary>
        public RunMapBatchResult GenerateBatch(int n)
        {
            if (n < 1) throw new ArgumentOutOfRangeException(nameof(n));
            EnsureConfigs();
            MapGenContext ctx = NewContext();
            var res = new RunMapBatchResult { Count = n, FirstSeed = Seed };
            long attempts = 0, rebuilds = 0;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < n; i++)
            {
                RunMap map = MapGenerator.Generate(unchecked(Seed + (ulong)i), ctx);
                attempts += map.GenerationAttempts;
                rebuilds += map.RowRebuilds;
                if (map.GenerationAttempts > res.MaxAttempts) res.MaxAttempts = map.GenerationAttempts;
                if (map.UsedFallback) res.FallbackCount++;
                List<string> v = MapConstraints.Validate(map, ctx);
                if (v.Count > 0)
                {
                    res.Violations++;
                    if (res.FirstViolation == null) res.FirstViolation = "seed " + map.Seed + ": " + v[0];
                }
                for (int r = 0; r < map.Rows.Length; r++)
                    for (int c = 0; c < map.Rows[r].Length; c++)
                        res.RoomCounts[(int)map.Rows[r][c].Type]++;
            }
            sw.Stop();
            res.MeanAttempts = (double)attempts / n;
            res.MeanRowRebuilds = (double)rebuilds / n;
            res.ElapsedMs = sw.Elapsed.TotalMilliseconds;
            return res;
        }

        /// <summary>
        /// Encounter plan for a Combat/Elite node of <see cref="CurrentMap"/>. PREVIEW STUB: the candidate pool is the
        /// Ashen Wastes bestiary of 09 §5.1 hardcoded here until the enemy definitions exist (M07/M16).
        /// </summary>
        public EncounterPlan PlanFor(MapNode node)
        {
            EnsureConfigs();
            bool elite = node.Type == RoomType.Elite;
            var req = new EncounterRequest
            {
                Row = node.Row,
                RoomType = node.Type,
                Difficulty = Difficulty,
                AscensionLevel = AscensionLevel,
                RegionId = RegionId ?? string.Empty,
                Pool = StubPool(elite),
            };
            ulong seed = unchecked((CurrentMap != null ? CurrentMap.Seed : Seed) * 31UL + (ulong)(node.Row * 8 + node.Column));
            return new EncounterPlanner(_encounterConfig).Plan(req, seed);
        }

        // 09 §5.1 Ashen Wastes (PA 1,3,4,5,8,6,5,5). The two Elites are test-only placeholders (as in the T4 fixture).
        private static List<EnemyCandidate> StubPool(bool withElite)
        {
            var l = new List<EnemyCandidate>
            {
                new EnemyCandidate("enemy.cinder_husk", EnemyArchetype.Fodder, 1),
                new EnemyCandidate("enemy.ashborn_marauder", EnemyArchetype.Standard, 3),
                new EnemyCandidate("enemy.slag_hurler", EnemyArchetype.Ranged, 4),
                new EnemyCandidate("enemy.choir_pyrecaller", EnemyArchetype.Caster, 5),
                new EnemyCandidate("enemy.kiln_brute", EnemyArchetype.Tank, 8),
                new EnemyCandidate("enemy.ember_matron", EnemyArchetype.Summoner, 6),
                new EnemyCandidate("enemy.cinder_stalker", EnemyArchetype.Assassin, 5),
                new EnemyCandidate("enemy.choir_cantor", EnemyArchetype.Support, 5),
            };
            if (withElite)
            {
                l.Add(new EnemyCandidate("enemy.elite_marauder", EnemyArchetype.Standard, 9, 1f, true));
                l.Add(new EnemyCandidate("enemy.elite_brute", EnemyArchetype.Tank, 14, 1f, true));
            }
            return l;
        }

        private MapGenContext NewContext() => new MapGenContext { RegionId = RegionId ?? string.Empty, Config = _mapConfig };

        private void EnsureConfigs()
        {
            if (_mapConfig != null && _encounterConfig != null) return;
            _mapConfig = null;
            _encounterConfig = null;
            string note = "Core defaults";
            try
            {
                string[] guids = AssetDatabase.FindAssets("t:DefinitionRegistry");
                if (guids.Length > 0)
                {
                    var reg = AssetDatabase.LoadAssetAtPath<DefinitionRegistry>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    if (reg != null)
                    {
                        IReadOnlyList<MapGenConfigDefinition> maps = reg.All<MapGenConfigDefinition>();
                        IReadOnlyList<EncounterConfigDefinition> encs = reg.All<EncounterConfigDefinition>();
                        IReadOnlyList<EncounterRecipeDefinition> recs = reg.All<EncounterRecipeDefinition>();
                        if (maps.Count > 0) { _mapConfig = MapGenConfigDefinition.ToCore(maps); note = "registry MapGen"; }
                        if (encs.Count > 0) { _encounterConfig = EncounterConfigDefinition.ToCore(encs, recs); note += (maps.Count > 0 ? " + " : "registry ") + "Encounter"; }
                        if (maps.Count > 0 && encs.Count > 0) note = "T5 definitions from the registry";
                        else if (maps.Count == 0 && encs.Count == 0) note = "Core defaults (no T5 definitions in the registry)";
                        else note += " (rest: Core defaults)";
                    }
                }
            }
            catch (Exception e)
            {
                _mapConfig = null;
                _encounterConfig = null;
                note = "Core defaults (registry config failed: " + e.Message + ")";
            }
            if (_mapConfig == null) _mapConfig = MapGenConfig.CreateDefault();
            if (_encounterConfig == null) _encounterConfig = EncounterConfig.Default();
            ConfigSource = note;
            if (_configLabel != null) _configLabel.text = "Config: " + ConfigSource;
        }

        // ------------------------------------------------------------------ drawing

        private void Redraw()
        {
            if (_graph == null || CurrentMap == null) return;
            _graph.Clear();
            _nodeButtons.Clear();
            _edges.Clear();
            _selected = null;
            if (_plan != null) _plan.text = "";

            MapNode[][] rows = CurrentMap.Rows;
            _graph.style.height = TopPad * 2 + RowStep * rows.Length;

            for (int r = 0; r < rows.Length; r++)
                for (int c = 0; c < rows[r].Length; c++)
                {
                    MapNode n = rows[r][c];
                    Vector2 p = NodePos(r, c, rows[r].Length);
                    var b = new Button { text = NodeText(n) };
                    b.AddToClassList("rmp-node");
                    b.AddToClassList(RoomClass(n.Type));
                    b.style.left = p.x - NodeW / 2f;
                    b.style.top = p.y - NodeH / 2f;
                    bool inMin = OnPath(MinElitePath, r, c), inMax = OnPath(MaxElitePath, r, c);
                    if (inMin && inMax) b.AddToClassList("path-both");
                    else if (inMin) b.AddToClassList("path-min");
                    else if (inMax) b.AddToClassList("path-max");
                    b.tooltip = Tooltip(n);
                    MapNode captured = n;
                    b.clicked += () => OnNodeClicked(captured, b);
                    _graph.Add(b);
                    _nodeButtons.Add(b);

                    for (int k = 0; k < n.Next.Length; k++)
                    {
                        int t = n.Next[k];
                        bool min = inMin && MinElitePath[r + 1] == t, max = inMax && MaxElitePath[r + 1] == t;
                        _edges.Add(new[] { p, NodePos(r + 1, t, rows[r + 1].Length), new Vector2(min ? 1 : 0, max ? 1 : 0) });
                    }
                }

            if (_summary != null)
            {
                int minE = MinElitePath != null ? MapPathAnalysis.CountElites(CurrentMap, MinElitePath) : -1;
                int maxE = MaxElitePath != null ? MapPathAnalysis.CountElites(CurrentMap, MaxElitePath) : -1;
                _summary.text = "Seed " + CurrentMap.Seed + "  |  hash " + CurrentMap.Hash().ToString("X16")
                    + "  |  attempts " + CurrentMap.GenerationAttempts + ", row rebuilds " + CurrentMap.RowRebuilds
                    + (CurrentMap.UsedFallback ? ", FALLBACK template" : "")
                    + "  |  Elites on min path " + minE + ", max path " + maxE;
            }
            _graph.MarkDirtyRepaint();
        }

        private static bool OnPath(int[] path, int r, int c) => path != null && path[r] == c;

        private static Vector2 NodePos(int row, int col, int count)
        {
            float x = GraphW / 2f + (col - (count - 1) / 2f) * ColStep;
            return new Vector2(x, TopPad + NodeH / 2f + row * RowStep);
        }

        private void DrawEdges(MeshGenerationContext ctx)
        {
            Painter2D p = ctx.painter2D;
            p.lineCap = LineCap.Round;
            // Plain edges first, highlighted ones on top.
            for (int pass = 0; pass < 3; pass++)
                for (int i = 0; i < _edges.Count; i++)
                {
                    Vector2[] e = _edges[i];
                    bool min = e[2].x > 0f, max = e[2].y > 0f;
                    int want = min ? 1 : max ? 2 : 0;
                    if (want != pass) continue;
                    Vector2 a = e[0], b = e[1];
                    if (min && max) { a.x -= 2f; b.x -= 2f; }
                    p.strokeColor = pass == 0 ? EdgeColor : pass == 1 ? PathMinColor : PathMaxColor;
                    p.lineWidth = pass == 0 ? 1.5f : 4f;
                    p.BeginPath();
                    p.MoveTo(a);
                    p.LineTo(b);
                    p.Stroke();
                    if (min && max)
                    {
                        // Shared edge: draw the red path next to the green one.
                        p.strokeColor = PathMaxColor;
                        p.BeginPath();
                        p.MoveTo(new Vector2(a.x + 4f, a.y));
                        p.LineTo(new Vector2(b.x + 4f, b.y));
                        p.Stroke();
                    }
                }
        }

        private void OnNodeClicked(MapNode node, Button button)
        {
            if (_selected != null) _selected.RemoveFromClassList("selected");
            _selected = button;
            button.AddToClassList("selected");
            if (_plan == null) return;
            if (node.Type != RoomType.Combat && node.Type != RoomType.Elite)
            {
                _plan.text = "Row " + node.Row + " " + node.Type + (node.EventId.Length > 0 ? " (" + node.EventId + ")" : "")
                    + ": no encounter plan (only Combat and Elite nodes).";
                return;
            }
            _plan.text = FormatPlan(node, PlanFor(node));
        }

        // ------------------------------------------------------------------ text

        private static string RoomClass(RoomType t) => "room-" + t.ToString().ToLowerInvariant();

        private static string RewardShort(DoorReward r)
        {
            switch (r)
            {
                case DoorReward.Equipment: return "Equip";
                case DoorReward.RiftShards: return "Rift";
                case DoorReward.AetherDust: return "Aether";
                case DoorReward.BlessingLesser: return "Bless";
                case DoorReward.Heal: return "Heal";
                default: return "";
            }
        }

        private static string NodeText(MapNode n)
        {
            string name = n.IsAntechamber ? "Antechamber" : n.Type.ToString();
            return n.Type == RoomType.Combat ? name + "\n[" + RewardShort(n.Reward) + "]" : name;
        }

        private static string Tooltip(MapNode n) =>
            "Row " + n.Row + ", col " + n.Column + " - " + n.Type
            + (n.Type == RoomType.Combat ? " - door: " + n.Reward : "")
            + (!string.IsNullOrEmpty(n.EventId) ? " - event: " + n.EventId : "");

        private static string FormatPlan(MapNode node, EncounterPlan plan)
        {
            var sb = new StringBuilder();
            sb.AppendLine("PREVIEW STUB: candidate pool = Ashen Wastes bestiary (09 5.1), not real enemy data (until M07/M16).");
            sb.AppendLine("Row " + node.Row + " " + node.Type + " | recipe " + (plan.RecipeId ?? "-"));
            sb.AppendLine("Budget " + plan.Budget.ToString("0.0") + " (spendable " + plan.SpendableBudget + ") | spent " + plan.TotalThreat
                + " PA | units " + plan.Units + (plan.UnderBudget ? " | UnderBudget" : "") + (plan.EliteShareMet ? "" : " | Elite share NOT met"));
            if (plan.Error != null) sb.AppendLine("Error: " + plan.Error);
            for (int w = 0; w < plan.Waves.Length; w++)
            {
                PlannedWave wave = plan.Waves[w];
                sb.Append("Wave " + (w + 1) + " (" + wave.Threat + "/" + wave.TargetThreat + " PA): ");
                var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
                for (int u = 0; u < wave.Units.Count; u++)
                {
                    string id = wave.Units[u].EnemyId.ToString();
                    counts.TryGetValue(id, out int c);
                    counts[id] = c + 1;
                }
                bool first = true;
                foreach (var kv in counts) { sb.Append(first ? "" : ", ").Append(kv.Value).Append("x ").Append(kv.Key); first = false; }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public static string FormatBatch(RunMapBatchResult r)
        {
            var sb = new StringBuilder();
            sb.AppendLine(r.Count + " seeds from " + r.FirstSeed + " in " + r.ElapsedMs.ToString("0") + " ms");
            sb.AppendLine("Violations: " + r.Violations + (r.Violations == 0 ? " (ok)" : " (FIRST: " + r.FirstViolation + ")"));
            sb.AppendLine("Attempts: mean " + r.MeanAttempts.ToString("0.000") + ", max " + r.MaxAttempts
                + " | row rebuilds mean " + r.MeanRowRebuilds.ToString("0.00") + " | fallback " + r.FallbackCount);
            int total = Math.Max(1, r.TotalRooms);
            foreach (RoomType t in Enum.GetValues(typeof(RoomType)))
            {
                int c = r.RoomCounts[(int)t];
                sb.AppendLine(t + ": " + c + " (" + (100.0 * c / total).ToString("0.0") + "%)");
            }
            return sb.ToString();
        }
    }
}

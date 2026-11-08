using System;
using System.IO;
using Eclipse.AI.Enemies;
using Eclipse.AI.Spawning;
using UnityEngine;

namespace Eclipse.AI.Diagnostics
{
    /// <summary>
    /// Coordinates the soak test (M07 T9, 11 section 3.14): idle GC baseline, pool warm-up, spawn of the 200 enemies (3 Cinder Husk : 1
    /// Ashborn Marauder = 150/50), a settle period that is not measured, then the measured window with the <see cref="SoakBot"/> fighting.
    /// The live count is topped up every frame (kills by the bot are replaced from the pools). In a player build it starts by itself
    /// (always, or only with <c>-soak</c> when <see cref="requireFlagInPlayer"/>), writes the CSV and quits; in the Editor a test
    /// calls <see cref="Begin"/>. Diagnostic only; the numbers are sandbox load settings, not balance data.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class SoakTestRunner : MonoBehaviour
    {
        public enum Phase { Idle, WaitReady, Baseline, Ramp, Settle, Measure, Done }

        [SerializeField] private AiSandboxBootstrap bootstrap;
        [SerializeField] private SimpleWaveSpawner spawner;
        [SerializeField] private SoakBot bot;
        [SerializeField] private GameObject huskPrefab;
        [SerializeField] private GameObject marauderPrefab;
        [SerializeField] private int targetAlive = 200;
        [Tooltip("One in every N spawns is a Marauder (4 gives 150 Husk : 50 Marauder).")]
        [SerializeField] private int marauderEvery = 4;
        [SerializeField] private int poolHeadroom = 30;
        [SerializeField] private int maxSpawnsPerFrame = 20;
        [SerializeField] private float spawnInnerRadius = 9f;
        [SerializeField] private float spawnOuterRadius = 27f;
        [SerializeField] private int baselineFrames = 120;
        [SerializeField] private float settleSeconds = 5f;
        [SerializeField] private float measureSeconds = 60f;
        [SerializeField] private bool quitWhenDone = true;
        [Tooltip("Player build: start only when the command line has -soak (otherwise start automatically).")]
        [SerializeField] private bool requireFlagInPlayer;
        [Tooltip("Frame cap while soaking in the player (vSync off), so the fixed 60 Hz simulation runs about once per frame; 0 leaves it uncapped.")]
        [SerializeField] private int targetFrameRate = 60;

        private PerfSampler _baseline, _measure;
        private Phase _phase = Phase.Idle;
        private float _phaseTime;
        private int _baselineLeft, _spawnIndex;
        private bool _csv = true;
        private string _csvPath;

        public Phase Current => _phase;
        public bool IsFinished => _phase == Phase.Done;
        public PerfSummary Result { get; private set; }
        public PerfSummary BaselineResult { get; private set; }
        public int Alive => spawner != null ? spawner.Alive : 0;
        public string CsvPath => _csvPath;

        /// <summary>Overrides the durations (the PlayMode test runs a short window) and the CSV target ("" disables the file).</summary>
        public void Configure(float settle, float measure, bool quit, string csvPath, int baselineFramesCount = 120)
        {
            settleSeconds = settle;
            measureSeconds = measure;
            quitWhenDone = quit;
            baselineFrames = baselineFramesCount;
            _csv = !string.IsNullOrEmpty(csvPath);
            _csvPath = csvPath;
        }

        public static string DefaultCsvPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "soak.csv"));

        public static string HardwareDescription =>
            "hardware: CPU " + SystemInfo.processorType + " x" + SystemInfo.processorCount + " | GPU " + SystemInfo.graphicsDeviceName
            + " | RAM " + SystemInfo.systemMemorySize + " MB | " + SystemInfo.operatingSystem
            + " | Unity " + Application.unityVersion + (Application.isEditor ? " Editor" : " player") + (Debug.isDebugBuild ? " (development)" : " (release)");

        private void Start()
        {
            if (string.IsNullOrEmpty(_csvPath)) _csvPath = DefaultCsvPath;
            if (Application.isEditor) return;
            if (requireFlagInPlayer && Array.IndexOf(Environment.GetCommandLineArgs(), "-soak") < 0) return;
            Begin();
        }

        public void Begin()
        {
            if (_phase != Phase.Idle) return;
            // The Editor allocates ~100 KB per frame while it sleeps to honour a frame cap (measured), which would drown the GC
            // numbers: the cap is for the player build only; the Editor runs uncapped (many frames per 60 Hz tick).
            if (!Application.isEditor)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = targetFrameRate > 0 ? targetFrameRate : -1;
            }
            int capacity = Mathf.CeilToInt((measureSeconds + 5f) * 4000f);
            _baseline = new PerfSampler(Mathf.Max(baselineFrames, 16));
            _measure = new PerfSampler(capacity);
            _phase = Phase.WaitReady;
        }

        private void OnDestroy()
        {
            _baseline?.Dispose();
            _measure?.Dispose();
        }

        private void Update()
        {
            switch (_phase)
            {
                case Phase.WaitReady:
                    if (bootstrap != null && bootstrap.IsReady && bot != null && bot.TryArm())
                    {
                        bot.enabled = false; // idle during the baseline
                        spawner.Warmup(huskPrefab, targetAlive * 3 / 4 + poolHeadroom);
                        spawner.Warmup(marauderPrefab, targetAlive / 4 + poolHeadroom);
                        _baseline.Begin();
                        _baselineLeft = baselineFrames;
                        Enter(Phase.Baseline);
                    }
                    break;

                case Phase.Baseline:
                    _baseline.Sample(Time.unscaledDeltaTime, Alive);
                    if (--_baselineLeft <= 0)
                    {
                        _baseline.End();
                        BaselineResult = _baseline.Summarize();
                        bot.enabled = true;
                        Enter(Phase.Ramp);
                    }
                    break;

                case Phase.Ramp:
                    TopUp();
                    if (Alive >= targetAlive) Enter(Phase.Settle);
                    break;

                case Phase.Settle:
                    TopUp();
                    _phaseTime += Time.unscaledDeltaTime;
                    if (_phaseTime >= settleSeconds)
                    {
                        _measure.Begin();
                        Enter(Phase.Measure);
                    }
                    break;

                case Phase.Measure:
                    TopUp();
                    _measure.Sample(Time.unscaledDeltaTime, Alive);
                    _phaseTime += Time.unscaledDeltaTime;
                    if (_phaseTime >= measureSeconds) Finish();
                    break;
            }
        }

        private void Enter(Phase p)
        {
            _phase = p;
            _phaseTime = 0f;
        }

        private void TopUp()
        {
            int deficit = targetAlive - spawner.Alive;
            if (deficit > maxSpawnsPerFrame) deficit = maxSpawnsPerFrame;
            for (int i = 0; i < deficit; i++)
            {
                int n = _spawnIndex++;
                // Deterministic scatter over the annulus (golden angle), no RNG in the spawn layout.
                float a = n * 2.39996323f;
                float f = (n * 0.61803399f) % 1f;
                float r = spawnInnerRadius + (spawnOuterRadius - spawnInnerRadius) * Mathf.Sqrt(f);
                var pos = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                GameObject prefab = (n % marauderEvery) == marauderEvery - 1 ? marauderPrefab : huskPrefab;
                spawner.SpawnAt(prefab, pos);
            }
        }

        private void Finish()
        {
            _measure.End();
            bot.Disarm();
            Result = _measure.Summarize();
            string hw = HardwareDescription;
            Debug.Log("[Soak] " + hw + "\n[Soak] baseline GC: median/p95 over idle frames; measured: " + Result);
            if (_csv)
            {
                try { _measure.WriteCsv(_csvPath, hw); }
                catch (Exception e) { Debug.LogError("[Soak] could not write " + _csvPath + ": " + e.Message); }
            }
            Enter(Phase.Done);
            if (quitWhenDone) Application.Quit(0);
        }

        /// <summary>Median of the idle-baseline GC samples (bytes), for the Editor noise comparison.</summary>
        public float[] BaselineGc()
        {
            var a = new float[_baseline.Count];
            Array.Copy(_baseline.GcSamples, a, a.Length);
            return a;
        }

        public float[] MeasuredGc()
        {
            var a = new float[_measure.Count];
            Array.Copy(_measure.GcSamples, a, a.Length);
            return a;
        }
    }
}

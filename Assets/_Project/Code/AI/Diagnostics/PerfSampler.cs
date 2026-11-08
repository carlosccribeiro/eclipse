using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;

namespace Eclipse.AI.Diagnostics
{
    /// <summary>Profiler markers of the AI layer (M07 T9); they cost nothing outside development builds and the Editor.</summary>
    public static class AiMarkers
    {
        public const string EnemyTickName = "Eclipse.AI.EnemyTick";
        public const string LocomotionName = "Eclipse.AI.Locomotion";
        public static readonly ProfilerMarker EnemyTick = new ProfilerMarker(ProfilerCategory.Scripts, EnemyTickName);
        public static readonly ProfilerMarker Locomotion = new ProfilerMarker(ProfilerCategory.Scripts, LocomotionName);
    }

    /// <summary>Distribution of one metric over the measured frames.</summary>
    public readonly struct MetricStats
    {
        public readonly double Mean, P95, P99, Max;

        public MetricStats(double mean, double p95, double p99, double max)
        {
            Mean = mean;
            P95 = p95;
            P99 = p99;
            Max = max;
        }
    }

    /// <summary>Result of a measured window (11 section 3.14 budget: frame 16.6 ms, gameplay+AI 5 ms, physics 2 ms, GC 0 B).</summary>
    public sealed class PerfSummary
    {
        public int Frames;
        public double Seconds;
        public MetricStats FrameMs, GameplayMs, AiTickMs, LocomotionMs, PhysicsMs, GcBytes;
        public int FramesAbove1KbGc, FramesWithGc, PeakAlive, MinAlive;
        public bool GameplayValid, PhysicsValid;

        public override string ToString() =>
            "frames " + Frames + " (" + Seconds.ToString("F1", CultureInfo.InvariantCulture) + " s) | frame ms mean " + F(FrameMs.Mean)
            + " p95 " + F(FrameMs.P95) + " p99 " + F(FrameMs.P99) + " max " + F(FrameMs.Max)
            + " | gameplay+AI ms mean " + F(GameplayMs.Mean) + " p95 " + F(GameplayMs.P95) + " p99 " + F(GameplayMs.P99)
            + " | AI tick ms mean " + F(AiTickMs.Mean) + " | locomotion ms mean " + F(LocomotionMs.Mean)
            + " | physics ms mean " + F(PhysicsMs.Mean) + " p95 " + F(PhysicsMs.P95)
            + " | GC B mean " + F(GcBytes.Mean) + " p95 " + F(GcBytes.P95) + " max " + F(GcBytes.Max)
            + " frames>1KB " + FramesAbove1KbGc + " frames>0B " + FramesWithGc + " | alive min " + MinAlive + " peak " + PeakAlive;

        private static string F(double v) => v.ToString("F2", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Per-frame performance collector (M07 T9). Frame time comes from the unscaled delta; "gameplay+AI" is the main-thread time of
    /// every MonoBehaviour FixedUpdate/Update/LateUpdate (the whole simulation scheduler runs inside those); the AI tick and the
    /// locomotion step have their own <see cref="AiMarkers"/>; physics is the engine's physics step; GC is "GC Allocated In Frame".
    /// Samples live in arrays allocated up front, so <see cref="Sample"/> itself allocates nothing. Physics queries issued by the
    /// scripts are part of the gameplay number (they run inside script callbacks).
    /// </summary>
    public sealed class PerfSampler : IDisposable
    {
        private readonly int _capacity;
        private readonly float[] _frame, _gameplay, _ai, _loco, _physics, _gc;
        private readonly int[] _alive;
        private ProfilerRecorder _fixedScripts, _updateScripts, _lateScripts, _aiTick, _locomotion, _physicsStep, _gcRec;
        private bool _started;

        public int Count { get; private set; }
        public bool GameplayValid { get; private set; }
        public bool PhysicsValid { get; private set; }
        public float[] GcSamples => _gc;

        public PerfSampler(int capacity)
        {
            _capacity = capacity;
            _frame = new float[capacity];
            _gameplay = new float[capacity];
            _ai = new float[capacity];
            _loco = new float[capacity];
            _physics = new float[capacity];
            _gc = new float[capacity];
            _alive = new int[capacity];
        }

        private static readonly List<ProfilerRecorderHandle> Handles = new List<ProfilerRecorderHandle>(4096);
        private static int _handlesCount = -1;

        private static ProfilerRecorder Start(string name, ProfilerCategory first, ProfilerCategory second)
        {
            const ProfilerRecorderOptions Opt = ProfilerRecorderOptions.Default | ProfilerRecorderOptions.SumAllSamplesInFrame;
            ProfilerRecorder r = ProfilerRecorder.StartNew(first, name, 4, Opt);
            if (r.Valid) return r;
            r.Dispose();
            r = ProfilerRecorder.StartNew(second, name, 4, Opt);
            if (r.Valid) return r;
            r.Dispose();
            // Fall back to the registry of every marker the engine exposes (matches by name, any category).
            ProfilerRecorderHandle.GetAvailable(Handles);
            _handlesCount = Handles.Count;
            for (int i = 0; i < Handles.Count; i++)
            {
                if (ProfilerRecorderHandle.GetDescription(Handles[i]).Name == name)
                    return new ProfilerRecorder(Handles[i], 4, Opt);
            }
            var sb = new StringBuilder("[PerfSampler] marker not found: " + name + ". Similar: ");
            string key = name.Substring(name.LastIndexOf('.') + 1);
            int shown = 0;
            for (int i = 0; i < Handles.Count && shown < 25; i++)
            {
                string n = ProfilerRecorderHandle.GetDescription(Handles[i]).Name;
                if (n.IndexOf(key.Length > 8 ? key.Substring(0, 8) : key, StringComparison.OrdinalIgnoreCase) >= 0) { sb.Append(n).Append(" | "); shown++; }
            }
            UnityEngine.Debug.LogWarning(sb.ToString() + " total handles " + Handles.Count);
            return default;
        }

        /// <summary>Starts the recorders and clears the samples. Call from the main thread.</summary>
        public void Begin()
        {
            End();
            AiMarkers.EnemyTick.Begin();
            AiMarkers.EnemyTick.End();
            _fixedScripts = Start("FixedUpdate.ScriptRunBehaviourFixedUpdate", ProfilerCategory.Scripts, ProfilerCategory.Internal);
            _updateScripts = Start("Update.ScriptRunBehaviourUpdate", ProfilerCategory.Scripts, ProfilerCategory.Internal);
            _lateScripts = Start("PreLateUpdate.ScriptRunBehaviourLateUpdate", ProfilerCategory.Scripts, ProfilerCategory.Internal);
            _aiTick = Start(AiMarkers.EnemyTickName, ProfilerCategory.Scripts, ProfilerCategory.Internal);
            _locomotion = Start(AiMarkers.LocomotionName, ProfilerCategory.Scripts, ProfilerCategory.Internal);
            _physicsStep = Start("Physics.Simulate", ProfilerCategory.Physics, ProfilerCategory.Internal);
            if (!_physicsStep.Valid)
            {
                _physicsStep.Dispose();
                _physicsStep = Start("FixedUpdate.PhysicsFixedUpdate", ProfilerCategory.Internal, ProfilerCategory.Physics);
            }
            GameplayValid = _fixedScripts.Valid || _updateScripts.Valid;
            PhysicsValid = _physicsStep.Valid;
            _gcRec = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 4);
            Count = 0;
            _started = true;
        }

        public void End()
        {
            if (!_started) return;
            _fixedScripts.Dispose();
            _updateScripts.Dispose();
            _lateScripts.Dispose();
            _aiTick.Dispose();
            _locomotion.Dispose();
            _physicsStep.Dispose();
            _gcRec.Dispose();
            _started = false;
        }

        public void Dispose() => End();

        /// <summary>Records the frame that just ended. Timing samples are the sums since the previous call.</summary>
        public void Sample(float frameSeconds, int alive)
        {
            if (!_started || Count >= _capacity) return;
            int i = Count++;
            _frame[i] = frameSeconds * 1000f;
            _gameplay[i] = Drain(ref _fixedScripts) + Drain(ref _updateScripts) + Drain(ref _lateScripts);
            _ai[i] = Drain(ref _aiTick);
            _loco[i] = Drain(ref _locomotion);
            _physics[i] = Drain(ref _physicsStep);
            _gc[i] = _gcRec.Valid ? _gcRec.LastValue : 0;
            _alive[i] = alive;
        }

        private static float Drain(ref ProfilerRecorder r)
        {
            if (!r.Valid) return 0f;
            return r.LastValue / 1000000f;
        }

        public PerfSummary Summarize()
        {
            var s = new PerfSummary
            {
                Frames = Count,
                FrameMs = Stats(_frame, Count),
                GameplayMs = Stats(_gameplay, Count),
                AiTickMs = Stats(_ai, Count),
                LocomotionMs = Stats(_loco, Count),
                PhysicsMs = Stats(_physics, Count),
                GcBytes = Stats(_gc, Count),
                GameplayValid = GameplayValid,
                PhysicsValid = PhysicsValid,
                MinAlive = Count > 0 ? int.MaxValue : 0,
            };
            double t = 0;
            for (int i = 0; i < Count; i++)
            {
                t += _frame[i];
                if (_gc[i] > 0) s.FramesWithGc++;
                if (_gc[i] > 1024) s.FramesAbove1KbGc++;
                if (_alive[i] > s.PeakAlive) s.PeakAlive = _alive[i];
                if (_alive[i] < s.MinAlive) s.MinAlive = _alive[i];
            }
            s.Seconds = t / 1000.0;
            return s;
        }

        public static MetricStats Stats(float[] data, int count)
        {
            if (count <= 0) return new MetricStats(0, 0, 0, 0);
            double sum = 0;
            var copy = new float[count];
            for (int i = 0; i < count; i++) { copy[i] = data[i]; sum += data[i]; }
            Array.Sort(copy);
            return new MetricStats(sum / count, Percentile(copy, 0.95), Percentile(copy, 0.99), copy[count - 1]);
        }

        /// <summary>Nearest-rank percentile of an ascending array.</summary>
        public static double Percentile(float[] sorted, double p)
        {
            if (sorted.Length == 0) return 0;
            int rank = (int)Math.Ceiling(p * sorted.Length);
            return sorted[Math.Max(0, Math.Min(sorted.Length - 1, rank - 1))];
        }

        /// <summary>Writes one row per second (mean and max of each metric) plus the SUMMARY block.</summary>
        public void WriteCsv(string path, string hardware)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var sb = new StringBuilder(8192);
            sb.AppendLine("# " + hardware);
            sb.AppendLine("second,frames,alive_min,frame_ms_mean,frame_ms_max,gameplay_ms_mean,gameplay_ms_max,ai_tick_ms_mean,locomotion_ms_mean,physics_ms_mean,physics_ms_max,gc_bytes_mean,gc_bytes_max");
            int start = 0;
            double acc = 0;
            int sec = 0;
            for (int i = 0; i < Count; i++)
            {
                acc += _frame[i];
                if (acc >= 1000.0 || i == Count - 1)
                {
                    AppendRow(sb, ++sec, start, i + 1);
                    start = i + 1;
                    acc = 0;
                }
            }
            PerfSummary s = Summarize();
            sb.AppendLine("# SUMMARY metric,mean,p95,p99,max");
            AppendStat(sb, "frame_ms", s.FrameMs);
            AppendStat(sb, "gameplay_ai_ms", s.GameplayMs);
            AppendStat(sb, "ai_tick_ms", s.AiTickMs);
            AppendStat(sb, "locomotion_ms", s.LocomotionMs);
            AppendStat(sb, "physics_ms", s.PhysicsMs);
            AppendStat(sb, "gc_bytes", s.GcBytes);
            sb.AppendLine("# frames," + s.Frames + ",seconds," + s.Seconds.ToString("F1", CultureInfo.InvariantCulture)
                + ",frames_gc_above_1KB," + s.FramesAbove1KbGc + ",frames_gc_nonzero," + s.FramesWithGc + ",alive_min," + s.MinAlive + ",alive_peak," + s.PeakAlive
                + ",gameplay_marker_valid," + s.GameplayValid + ",physics_marker_valid," + s.PhysicsValid);
            File.WriteAllText(path, sb.ToString());
        }

        private void AppendRow(StringBuilder sb, int second, int from, int to)
        {
            int minAlive = int.MaxValue;
            for (int i = from; i < to; i++) if (_alive[i] < minAlive) minAlive = _alive[i];
            sb.Append(second).Append(',').Append(to - from).Append(',').Append(minAlive);
            AppendMeanMax(sb, _frame, from, to, true);
            AppendMeanMax(sb, _gameplay, from, to, true);
            AppendMeanMax(sb, _ai, from, to, false);
            AppendMeanMax(sb, _loco, from, to, false);
            AppendMeanMax(sb, _physics, from, to, true);
            AppendMeanMax(sb, _gc, from, to, true);
            sb.AppendLine();
        }

        private static void AppendMeanMax(StringBuilder sb, float[] a, int from, int to, bool withMax)
        {
            double sum = 0;
            float max = 0;
            for (int i = from; i < to; i++) { sum += a[i]; if (a[i] > max) max = a[i]; }
            sb.Append(',').Append((sum / Math.Max(1, to - from)).ToString("F3", CultureInfo.InvariantCulture));
            if (withMax) sb.Append(',').Append(max.ToString("F3", CultureInfo.InvariantCulture));
        }

        private static void AppendStat(StringBuilder sb, string name, MetricStats m) =>
            sb.Append(name).Append(',').Append(m.Mean.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
              .Append(m.P95.ToString("F3", CultureInfo.InvariantCulture)).Append(',').Append(m.P99.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
              .Append(m.Max.ToString("F3", CultureInfo.InvariantCulture)).AppendLine();
    }
}

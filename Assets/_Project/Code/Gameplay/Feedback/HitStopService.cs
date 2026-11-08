using System.Collections.Generic;
using Eclipse.Gameplay.Simulation;

namespace Eclipse.Gameplay.Feedback
{
    /// <summary>Result of <see cref="HitStopService.Plan"/>: frames to freeze each side (0 = none).</summary>
    public readonly struct HitStopPlan
    {
        public readonly int AttackerFrames, TargetFrames;
        public HitStopPlan(int attacker, int target) { AttackerFrames = attacker; TargetFrames = target; }
        public bool Any => AttackerFrames > 0 || TargetFrames > 0;
    }

    /// <summary>
    /// Hit-stop policy (06 3.11): local on attacker + target; crit adds the profile bonus; Elite kill 8 f; global 6 f;
    /// at most one hit-stop per attacker per spacing (100 ms = 6 ticks); projectiles freeze only the target.
    /// Pure C#; no allocation after the first hit of each attacker.
    /// </summary>
    public sealed class HitStopService
    {
        private readonly FeedbackRules _rules;
        private readonly Dictionary<int, int> _lastFrame = new Dictionary<int, int>(64);

        public HitStopService(FeedbackRules rules) { _rules = rules; }

        /// <summary>Milliseconds to ticks, rounded up (100 ms -> 6).</summary>
        public static int MsToFrames(int ms) => ms <= 0 ? 0 : (ms * SimulationFrame.TicksPerSecond + 999) / 1000;

        /// <summary>Decides the freeze of one hit and registers it for the spacing rule.</summary>
        public HitStopPlan Plan(int attacker, int frame, in HitFeedbackInfo info, bool isCrit)
        {
            int frames = info.HitStopFrames;
            if (frames <= 0) return default;
            if (_lastFrame.TryGetValue(attacker, out int last) && frame - last < MsToFrames(info.Values.MinHitStopSpacingMs))
                return default;
            _lastFrame[attacker] = frame;
            if (isCrit) frames += info.Values.CritHitStopBonusFrames;
            return new HitStopPlan(info.IsProjectile ? 0 : frames, frames);
        }

        /// <summary>Elite kill: frames to freeze the attacker (EntityClock keeps the longest, no stacking).</summary>
        public int EliteKillFrames(int attacker, int frame)
        {
            _lastFrame[attacker] = frame;
            return _rules.EliteKillHitStopFrames;
        }

        public void Reset() => _lastFrame.Clear();

        public static void Freeze(EntityClock clock, int frames)
        {
            if (clock != null && frames > 0) clock.Freeze(frames);
        }
    }
}

using Eclipse.Gameplay.Simulation;
using UnityEngine;

namespace Eclipse.Gameplay.Player
{
    /// <summary>
    /// Universal dodge data (06 section 3.7, 00 section 5 R06). Identical for every hero; all values live here.
    /// Asset: Content/Heroes/Shared/Data/DodgeConfig_Universal.asset (created by DodgeConfigCreator).
    /// Frames are 1-based dodge frames at 60 Hz; the tick that starts the dodge is frame 1.
    /// </summary>
    [CreateAssetMenu(menuName = "Eclipse/Dodge Config", fileName = "DodgeConfig")]
    public sealed class DodgeConfig : ScriptableObject
    {
        /// <summary>Total duration in frames (06 3.7: 18 f).</summary>
        [Min(1)] public int totalFrames = 18;

        /// <summary>Fixed distance in meters (06 3.7: 4.5 m). Movement Speed does not change it.</summary>
        [Min(0f)] public float distanceMeters = 4.5f;

        /// <summary>Ease-out exponent: progress(t) = 1 - (1 - t)^power. 2 = quadratic ease-out.</summary>
        [Min(1f)] public float easeOutPower = 2f;

        /// <summary>First invulnerable dodge frame, inclusive (06 3.7: 2).</summary>
        [Min(1)] public int invulnerableStartFrame = 2;

        /// <summary>Last invulnerable dodge frame, inclusive (06 3.7: 13).</summary>
        [Min(1)] public int invulnerableEndFrame = 13;

        /// <summary>Recovery frames at the end of the dodge (06 3.7: 3).</summary>
        [Min(0)] public int recoveryFrames = 3;

        /// <summary>First dodge frame from which attacks/abilities may cancel the dodge (06 3.7: 15).</summary>
        [Min(1)] public int cancelFromFrame = 15;

        /// <summary>Base charges (R06: 2).</summary>
        [Min(1)] public int baseCharges = 2;

        /// <summary>Absolute cap, reachable only through explicit item/talent sources (06 3.7: 3).</summary>
        [Min(1)] public int maxChargesCap = 3;

        /// <summary>Recharge time per charge in seconds, sequential (06 3.7: 1.5 s). Not affected by CDR.</summary>
        [Min(0.01f)] public float rechargeSeconds = 1.5f;

        /// <summary>
        /// Perfect Dodge window, in frames counted from the first invulnerable frame (inclusive).
        /// Interpretation: an impact on frames [start, start + PerfectDodgeWindowFrames - 1] of the i-frames is perfect.
        /// </summary>
        [Min(0)] public int perfectDodgeWindowFrames = 6;

        /// <summary>Recharge per charge in 60 Hz ticks (1.5 s = 90).</summary>
        public int RechargeTicks => Mathf.Max(1, Mathf.RoundToInt(rechargeSeconds * SimulationFrame.TicksPerSecond));

        /// <summary>Ease-out progress (0..1) for normalized time t (0..1).</summary>
        public float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, easeOutPower);
        }

        /// <summary>Distance in meters covered during dodge frame <paramref name="frame"/> (1-based).</summary>
        public float StepDistance(int frame)
        {
            if (frame < 1 || frame > totalFrames) return 0f;
            return distanceMeters * (EaseOut(frame / (float)totalFrames) - EaseOut((frame - 1) / (float)totalFrames));
        }
    }
}

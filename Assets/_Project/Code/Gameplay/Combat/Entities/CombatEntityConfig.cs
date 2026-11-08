using Eclipse.Core.Damage;
using Eclipse.Gameplay.Simulation;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Injected tuning of <see cref="CombatEntity"/>. No balance default lives in code (CLAUDE.md 1): every field is
    /// filled by the owner from data (06 3.3, 3.12, 3.14; 01 3.4.1). Zero means "feature off".
    /// </summary>
    public sealed class CombatEntityConfig
    {
        /// <summary>Caps of the incoming pipeline (01 3.4.1, 3.6: Dodge 40%, Block 50%, Damage Reduction 40%).</summary>
        public MitigationRules Mitigation;
        /// <summary>Damage removed from a blocked hit, fraction (06 3.3 step 4 / 01: 0.40).</summary>
        public float BlockAmount;
        /// <summary>Resistance penalty of the difficulty, fraction (01 3.4.1).</summary>
        public float DifficultyResPenalty;
        /// <summary>Grace after any landed hit, in frames (06 3.14: 0.25 s).</summary>
        public int GraceFrames;
        /// <summary>Grace after a hit of at least <see cref="HeavyHitHealthFraction"/> of max health (06 3.14: 0.8 s).</summary>
        public int HeavyGraceFrames;
        /// <summary>Fraction of max health that makes a hit "heavy" for grace (06 3.14: 0.20).</summary>
        public float HeavyHitHealthFraction;
        /// <summary>Minimum frames between ticks of the same source (06 3.14: 0.5 s).</summary>
        public int TickIntervalFrames;
        /// <summary>OnLowHealth threshold, fraction of max health (data; 0 = off).</summary>
        public float LowHealthFraction;
        /// <summary>Presentation-only time scale while dying (06 3.14: 0.3); simulation logic pauses.</summary>
        public float DeathSlowMotionScale = 1f;
        /// <summary>Presentation-only duration of the death slow motion, in frames (06 3.14: 0.5 s).</summary>
        public int DeathSlowMotionFrames;
        /// <summary>Health fraction on revive (06 3.14: 0.5).</summary>
        public float ReviveHealthFraction;
        /// <summary>Total invulnerability on revive, in frames (06 3.14: 3 s).</summary>
        public int ReviveInvulnerabilityFrames;
        /// <summary>Max revives per boss room (06 3.14: 1).</summary>
        public int MaxRevivesPerBossRoom;

        /// <summary>Seconds to 60 Hz frames (06 2.1).</summary>
        public static int SecondsToFrames(float seconds) => (int)(seconds * SimulationFrame.TicksPerSecond + 0.5f);
    }
}

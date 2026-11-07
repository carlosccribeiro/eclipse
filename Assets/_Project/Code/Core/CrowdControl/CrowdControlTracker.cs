using System;

namespace Eclipse.Core.CrowdControl
{
    /// <summary>
    /// Per-entity CC diminishing returns (06 §3.8.3, R09). Time unit: simulation ticks (60 Hz);
    /// the returned duration is in whole ticks.
    /// Order: base seconds x (1 - StatusResistance) -> enemy DR multiplier (enemies only) -> ceil to ticks
    /// -> player caps (players only). Enemies: window opens at the first CC of the group; 1st/2nd/3rd get
    /// 100/50/25%; later ones are immune (return 0, do not count) until the window ends. Player: no sequence,
    /// only caps (Hard 1.5 s, Freeze 1.0 s, Root 2 s) and a Hard immunity counted from the END of the applied Hard.
    /// Allocation-free after construction. Not thread-safe.
    /// </summary>
    public sealed class CrowdControlTracker
    {
        private const int GroupCount = 4;
        private readonly CcRules _rules;
        private readonly long[] _windowStart = new long[GroupCount];
        private readonly int[] _count = new int[GroupCount];
        private long _hardImmuneUntil; // exclusive tick; player only

        public CrowdControlTracker(CcRules rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Reset();
        }

        /// <summary>Clears all windows and immunities.</summary>
        public void Reset()
        {
            for (int i = 0; i < GroupCount; i++) { _windowStart[i] = 0; _count[i] = 0; }
            _hardImmuneUntil = long.MinValue;
        }

        /// <summary>True if a new CC of the group would currently be ignored at <paramref name="nowTick"/> (immunity or DR exhausted).</summary>
        public bool IsImmune(CcGroup group, bool isPlayer, long nowTick)
        {
            int g = (int)group;
            if (isPlayer)
                return group == CcGroup.Taunt || (group == CcGroup.Hard && nowTick < _hardImmuneUntil);
            return _count[g] >= _rules.SequenceLength && nowTick < _windowStart[g] + _rules.WindowTicks;
        }

        /// <summary>
        /// Requests a CC. <paramref name="baseDurationSeconds"/> is the nominal duration; <paramref name="statusResistance"/>
        /// is a fraction (0.3 = 30%, clamped to [0,1]; pass 0 for enemies). Returns the applied duration in ticks
        /// (0 = immune/ignored, which consumes no DR). Taunt is never applied to the player.
        /// </summary>
        public int TryApply(CcKind kind, float baseDurationSeconds, bool isPlayer, float statusResistance, long nowTick)
        {
            if (float.IsNaN(baseDurationSeconds) || float.IsInfinity(baseDurationSeconds) || baseDurationSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(baseDurationSeconds));
            if (float.IsNaN(statusResistance)) throw new ArgumentOutOfRangeException(nameof(statusResistance));
            if (baseDurationSeconds == 0f) return 0;

            CcGroup group = kind.Group();
            int g = (int)group;
            double seconds = baseDurationSeconds * (1.0 - (statusResistance < 0f ? 0f : statusResistance > 1f ? 1f : statusResistance));

            if (isPlayer)
            {
                if (group == CcGroup.Taunt) return 0;
                if (group == CcGroup.Hard && nowTick < _hardImmuneUntil) return 0;
                int ticks = CcRules.SecondsToTicksCeil(seconds);
                int cap = int.MaxValue;
                if (kind == CcKind.Freeze) cap = _rules.PlayerFreezeMaxTicks;
                else if (group == CcGroup.Hard) cap = _rules.PlayerHardMaxTicks;
                else if (group == CcGroup.Root) cap = _rules.PlayerRootMaxTicks;
                if (ticks > cap) ticks = cap;
                if (ticks > 0 && group == CcGroup.Hard)
                    _hardImmuneUntil = nowTick + ticks + _rules.PlayerHardImmunityTicks; // immunity starts when the Hard ends
                return ticks;
            }

            bool newWindow = _count[g] == 0 || nowTick >= _windowStart[g] + _rules.WindowTicks;
            int index = newWindow ? 0 : _count[g];
            float mult = _rules.SequenceMultiplier(index);
            if (mult <= 0f) return 0;
            int result = CcRules.SecondsToTicksCeil(seconds * mult);
            if (result <= 0) return 0;
            if (newWindow) { _windowStart[g] = nowTick; _count[g] = 0; }
            _count[g]++;
            return result;
        }
    }
}

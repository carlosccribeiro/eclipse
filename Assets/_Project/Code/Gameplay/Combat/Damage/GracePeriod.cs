namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Grace period after a received hit and per-source tick limiter (06 3.14). Durations come from
    /// <see cref="CombatEntityConfig"/>. Frames are global simulation frames. Allocation-free.
    /// </summary>
    public sealed class GracePeriod
    {
        // Tolerance for the "at least 20%" comparison (float fractions such as 0.2f are not exact).
        private const float FractionEpsilon = 1e-5f;
        private const int TickSources = 16; // structural: tracked DoT/zone sources per entity

        private readonly CombatEntityConfig _cfg;
        private readonly int[] _srcId = new int[TickSources];
        private readonly int[] _srcFrame = new int[TickSources];
        private int _srcCount;
        private int _untilFrame; // exclusive: hits at frame < _untilFrame are blocked

        public GracePeriod(CombatEntityConfig cfg) { _cfg = cfg; }

        public bool IsActive(int frame) => frame < _untilFrame;

        /// <summary>Starts grace after a landed (non-tick) hit; heavy grace when damage is at least the configured health fraction.</summary>
        public void OnHitLanded(int frame, float damage, float maxHealth)
        {
            bool heavy = _cfg.HeavyHitHealthFraction > 0f && damage / maxHealth + FractionEpsilon >= _cfg.HeavyHitHealthFraction;
            int until = frame + (heavy ? _cfg.HeavyGraceFrames : _cfg.GraceFrames);
            if (until > _untilFrame) _untilFrame = until;
        }

        /// <summary>True (and registers) if the source may tick now: at most one tick per source per interval. Ticks ignore grace.</summary>
        public bool AllowsTick(int frame, int sourceId)
        {
            for (int i = 0; i < _srcCount; i++)
            {
                if (_srcId[i] != sourceId) continue;
                if (frame - _srcFrame[i] < _cfg.TickIntervalFrames) return false;
                _srcFrame[i] = frame;
                return true;
            }
            int slot;
            if (_srcCount < TickSources) slot = _srcCount++;
            else
            {
                slot = 0;
                for (int i = 1; i < TickSources; i++) if (_srcFrame[i] < _srcFrame[slot]) slot = i;
            }
            _srcId[slot] = sourceId;
            _srcFrame[slot] = frame;
            return true;
        }

        public void Reset() { _untilFrame = 0; _srcCount = 0; }
    }
}

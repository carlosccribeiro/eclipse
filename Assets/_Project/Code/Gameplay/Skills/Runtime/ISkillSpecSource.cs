using Eclipse.Core.Stats;
using Eclipse.Data.Skills;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>Provides the compiled spec of each slot to the controller.</summary>
    public interface ISkillSpecSource
    {
        /// <summary>Compiled spec of the slot, or null when the hero has no skill there.</summary>
        SkillSpec Get(SkillSlot slot);
    }

    /// <summary>
    /// Caches one <see cref="SkillSpec"/> per slot and recompiles it on a frame after a <see cref="StatsChanged"/> touching
    /// CDR, cost reduction or Skill Damage (11 3.5.3: next frame, never the same frame as the change).
    /// </summary>
    public sealed class SkillSpecCache : ISkillSpecSource
    {
        private static readonly StatMask Watched =
            StatMask.Of(StatId.CooldownReduction).With(StatId.ResourceCostReduction).With(StatId.SkillDamage);

        private readonly StatBlock _stats;
        private readonly SkillSpecBuilder[] _builders = new SkillSpecBuilder[7];
        private readonly SkillSpec[] _specs = new SkillSpec[7];
        private int _dirtyFrame = -1;

        public SkillSpecCache(StatBlock stats)
        {
            _stats = stats;
            _stats?.Changed.Subscribe(OnStatsChanged);
        }

        public bool IsDirty { get; private set; }

        public void Register(SkillSpecBuilder builder)
        {
            _builders[(int)builder.Slot] = builder;
            _specs[(int)builder.Slot] = builder.Build(_stats);
        }

        public void MarkDirty() => IsDirty = true;

        /// <summary>Call once per simulation frame; recompiles on the first frame after the one that saw the change.</summary>
        public void Tick(int frame)
        {
            if (!IsDirty) return;
            if (_dirtyFrame < 0) { _dirtyFrame = frame; return; }
            if (frame <= _dirtyFrame) return;
            for (int i = 0; i < _builders.Length; i++)
                if (_builders[i] != null) _specs[i] = _builders[i].Build(_stats);
            IsDirty = false;
            _dirtyFrame = -1;
        }

        public SkillSpec Get(SkillSlot slot) => _specs[(int)slot];

        private void OnStatsChanged(in StatsChanged e)
        {
            if (e.Mask.HasAny(Watched)) IsDirty = true;
        }
    }
}

using Eclipse.Data.Skills;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>Per-slot cooldowns in simulation frames. No allocation. Dodge charges are tracked elsewhere (DodgeController) and ignore CDR.</summary>
    public sealed class CooldownTracker
    {
        // Eclipse.Data.Skills.SkillSlot has 7 values.
        private const int SlotCount = 7;
        private readonly int[] _readyAt = new int[SlotCount];

        public void Start(SkillSlot slot, int frame, int cooldownFrames)
        {
            _readyAt[(int)slot] = cooldownFrames <= 0 ? 0 : frame + cooldownFrames;
        }

        public bool IsReady(SkillSlot slot, int frame) => frame >= _readyAt[(int)slot];

        public int RemainingFrames(SkillSlot slot, int frame)
        {
            int r = _readyAt[(int)slot] - frame;
            return r > 0 ? r : 0;
        }

        public void Reset(SkillSlot slot) => _readyAt[(int)slot] = 0;

        public void ResetAll()
        {
            for (int i = 0; i < _readyAt.Length; i++) _readyAt[i] = 0;
        }
    }
}

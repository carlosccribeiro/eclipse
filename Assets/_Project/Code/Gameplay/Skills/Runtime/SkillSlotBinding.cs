using Eclipse.Data.Skills;
using Eclipse.Gameplay.Input;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>
    /// Slot map between hero skill slots and buffered input (00 R05, 06 3.4): Q/E/R = Skill1-3, Shift = Skill4, F = Ultimate.
    /// Basic and Heavy are weapon attacks and never go through the SkillController.
    /// </summary>
    public static class SkillSlotBinding
    {
        /// <summary>Bit mask of the buffered actions the SkillController consumes.</summary>
        public static readonly uint AcceptedMask =
            InputActionId.Skill1.Bit() | InputActionId.Skill2.Bit() | InputActionId.Skill3.Bit() |
            InputActionId.Skill4.Bit() | InputActionId.Ultimate.Bit();

        public static bool TryGetSlot(InputActionId id, out SkillSlot slot)
        {
            switch (id)
            {
                case InputActionId.Skill1: slot = SkillSlot.Q; return true;
                case InputActionId.Skill2: slot = SkillSlot.E; return true;
                case InputActionId.Skill3: slot = SkillSlot.R; return true;
                case InputActionId.Skill4: slot = SkillSlot.Shift; return true;
                case InputActionId.Ultimate: slot = SkillSlot.F; return true;
                default: slot = default; return false;
            }
        }

        public static bool TryGetInput(SkillSlot slot, out InputActionId id)
        {
            switch (slot)
            {
                case SkillSlot.Q: id = InputActionId.Skill1; return true;
                case SkillSlot.E: id = InputActionId.Skill2; return true;
                case SkillSlot.R: id = InputActionId.Skill3; return true;
                case SkillSlot.Shift: id = InputActionId.Skill4; return true;
                case SkillSlot.F: id = InputActionId.Ultimate; return true;
                default: id = InputActionId.None; return false;
            }
        }
    }
}

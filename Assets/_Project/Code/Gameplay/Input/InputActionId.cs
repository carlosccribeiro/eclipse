namespace Eclipse.Gameplay.Input
{
    /// <summary>
    /// Slot-level identifiers of buffered player actions (06 §3.4, 00 §5 R05). The input layer never knows
    /// which ability sits in a slot; Skill1-4 are slots (Q/E/R/Shift or LB/RB/LT/RT).
    /// </summary>
    public enum InputActionId
    {
        None = 0,
        Basic = 1,
        Heavy = 2,
        Dodge = 3,
        Skill1 = 4,
        Skill2 = 5,
        Skill3 = 6,
        Skill4 = 7,
        Ultimate = 8,
        Flask = 9,
        Interact = 10,
    }

    public static class InputActionIdExtensions
    {
        public const uint AllMask = uint.MaxValue;

        /// <summary>
        /// Buffer priority (06 §3.4: Dodge > Ability > Heavy > Basic). Ability class = Skill1-4, Ultimate, Flask.
        /// Returns -1 for actions that are not buffered (Interact, None).
        /// </summary>
        public static int BufferPriority(this InputActionId id)
        {
            switch (id)
            {
                case InputActionId.Dodge: return 3;
                case InputActionId.Skill1:
                case InputActionId.Skill2:
                case InputActionId.Skill3:
                case InputActionId.Skill4:
                case InputActionId.Ultimate:
                case InputActionId.Flask: return 2;
                case InputActionId.Heavy: return 1;
                case InputActionId.Basic: return 0;
                default: return -1;
            }
        }

        /// <summary>Single-bit mask for use with <see cref="InputBuffer.TryConsume(int, uint, out BufferedAction)"/>.</summary>
        public static uint Bit(this InputActionId id) => 1u << (int)id;
    }
}

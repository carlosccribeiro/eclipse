namespace Eclipse.AI.Director
{
    /// <summary>Which pool a held token came from.</summary>
    public enum TokenPool : byte
    {
        None = 0,
        Melee = 1,
        Ranged = 2,
        Ambush = 3,
        /// <summary>Attack allowed without consuming a token (Elite, Guardian, Boss, Taunt).</summary>
        Free = 4
    }

    /// <summary>Per-enemy record kept by the director (pre-allocated array element).</summary>
    public struct TokenTicket
    {
        public bool Used;
        public int OwnerId;
        public TokenClass Class;
        public bool Taunted;
        public TokenPool Held;
        /// <summary>Time of the last granted attack; only valid when <see cref="HasGranted"/>.</summary>
        public double LastGrantTime;
        public bool HasGranted;
    }
}

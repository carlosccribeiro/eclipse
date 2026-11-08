namespace Eclipse.Core.Loot
{
    /// <summary>The 10 loot sources of 05 section 3.1.</summary>
    public enum LootSourceKind : byte
    {
        Trash, Elite, Guardian, ArenaReward, TreasureDelivered, Reliquary, Challenge, Event, Boss, RiftCache
    }

    /// <summary>Difficulty tier of 05 (Ascension is Difficulty.Ascension plus <see cref="LootContext.AscensionLevel"/> 1-100).</summary>
    public enum Difficulty : byte { Normal, Veteran, Nightmare, Eclipse, Ascension }

    /// <summary>Kind of Attunement target (05 section 9.4).</summary>
    public enum AttunementKind : byte { None, Set, Unique }
}

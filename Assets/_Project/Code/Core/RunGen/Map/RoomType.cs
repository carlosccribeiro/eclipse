namespace Eclipse.Core.RunGen.Map
{
    /// <summary>
    /// Room types of the run map (09 §9). The Antechamber is not a type of its own: it is
    /// <see cref="Rest"/> with <see cref="MapNode.IsAntechamber"/> set (11 §3.4.2).
    /// Order is part of <see cref="MapGenConfig"/> weight tables and the map hash: only append.
    /// </summary>
    public enum RoomType : byte
    {
        Combat,
        Elite,
        Guardian,
        Treasure,
        Shrine,
        Event,
        Challenge,
        Rest,
        Boss
    }
}

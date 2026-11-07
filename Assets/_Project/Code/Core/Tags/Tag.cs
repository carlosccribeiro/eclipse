namespace Eclipse.Core.Tags
{
    /// <summary>
    /// Ability tag vocabulary (00-convencoes 3.4). Values are bit indices (0..23) in <see cref="TagMask"/>.
    /// New tags may only be added through 11-arquitetura. Hit flags (R02) are NOT tags: see <see cref="HitFlags"/>.
    /// </summary>
    public enum Tag : byte
    {
        Attack = 0,
        Spell = 1,
        Melee = 2,
        Ranged = 3,
        Projectile = 4,
        Area = 5,
        DoT = 6,
        Summon = 7,
        Construct = 8,
        Movement = 9,
        Defensive = 10,
        Ultimate = 11,
        Channel = 12,
        Trap = 13,
        Clone = 14,
        Basic = 15,
        Heavy = 16,
        Ember = 17,
        Frost = 18,
        Storm = 19,
        Void = 20,
        Echo = 21,
        Ruin = 22,
        Physical = 23,
    }
}

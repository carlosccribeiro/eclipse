namespace Eclipse.Core.RunGen.Map
{
    /// <summary>
    /// Reward shown on the door of a Combat Arena (09 §9, Combat Arena row). Only Combat nodes carry
    /// a reward; every other node uses <see cref="None"/>. Order is part of the map hash: only append.
    /// </summary>
    public enum DoorReward : byte
    {
        None,
        Equipment,
        RiftShards,
        AetherDust,
        BlessingLesser,
        Heal
    }
}

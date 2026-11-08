namespace Eclipse.AI.Lod
{
    /// <summary>Decision-rate LOD settings (09 section 3.1, 11 section 3.10).</summary>
    public sealed class DecisionLodConfig
    {
        /// <summary>Design rate for near / on-screen enemies (09 section 3.1).</summary>
        public const float DesignNearHz = 10f;
        /// <summary>Design rate for far / off-screen enemies (09 section 3.1).</summary>
        public const float DesignFarHz = 4f;

        /// <summary>Within this distance (m) of the player an enemy is "near". Set from data by the caller.</summary>
        public float NearRadius;
        public float NearHz = DesignNearHz;
        public float FarHz = DesignFarHz;
    }
}

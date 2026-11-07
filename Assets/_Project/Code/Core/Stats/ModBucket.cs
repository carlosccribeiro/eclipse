namespace Eclipse.Core.Stats
{
    /// <summary>Modifier bucket (11 3.5.1). No Override bucket (00 5 R47).</summary>
    public enum ModBucket
    {
        /// <summary>Added to the base before multipliers.</summary>
        Flat = 0,
        /// <summary>Summed with other Increased, applied as (1 + sum).</summary>
        Increased = 1,
        /// <summary>Each applied separately as (1 + value).</summary>
        More = 2,
    }
}

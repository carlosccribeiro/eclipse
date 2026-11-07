namespace Eclipse.Core.CrowdControl
{
    /// <summary>Diminishing-returns group (06 §3.8.3). Each group has its own independent DR window.</summary>
    public enum CcGroup
    {
        /// <summary>Stun and Freeze share this group.</summary>
        Hard = 0,
        Root = 1,
        Silence = 2,
        Taunt = 3,
    }
}

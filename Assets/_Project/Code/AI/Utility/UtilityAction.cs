namespace Eclipse.AI.Utility
{
    /// <summary>Actions the Utility selector can pick (11 section 3.10; Recover/Dead are HFSM-only).</summary>
    public enum UtilityAction : byte
    {
        None = 0,
        Approach,
        Reposition,
        Attack,
        Flee
    }
}

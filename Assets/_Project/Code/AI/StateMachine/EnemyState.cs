namespace Eclipse.AI.StateMachine
{
    /// <summary>
    /// HFSM leaf states (11 section 3.10): Idle, Engage (parent of Approach | Reposition | Attack | Recover | Flee), Dead.
    /// Engage as a leaf means "engaged, waiting for the next decision".
    /// </summary>
    public enum EnemyState : byte
    {
        Idle = 0,
        Engage,
        Approach,
        Reposition,
        Attack,
        Recover,
        Flee,
        Dead
    }
}

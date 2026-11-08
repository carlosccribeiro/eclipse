using Eclipse.AI.Utility;

namespace Eclipse.AI.StateMachine
{
    /// <summary>Brain output for one tick.</summary>
    public struct AiDecision
    {
        public EnemyState State;
        public EnemyState PreviousState;
        public bool StateChanged;
        /// <summary>True on ticks where the LOD allowed a new decision.</summary>
        public bool Evaluated;
        public UtilityAction Action;
        /// <summary>Index into UtilityConfig.Attacks when Action is Attack, else -1.</summary>
        public int AttackIndex;
        public float Score;
        /// <summary>True when the weighted-random branch (not the best action) was taken.</summary>
        public bool Randomized;
        /// <summary>True only on the tick the Attack state begins: the caller starts the cooldown/telegraph.</summary>
        public bool AttackStarted;
    }
}

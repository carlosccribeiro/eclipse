namespace Eclipse.AI.Enemies
{
    /// <summary>
    /// Ranged retreat (09 section 4.2): when the target gets closer than <c>retreat_below_m</c> the enemy backs off until it is
    /// back inside its preferred band (or the cooldown span passes), and cannot retreat again for <c>retreat_cooldown_s</c> counted from the start of the
    /// retreat. Both distances and the cooldown come from <c>EnemyDefinition</c>; 0 disables it. Pure, no allocation.
    /// </summary>
    public sealed class RetreatPlanner
    {
        private readonly float _belowMeters;
        private readonly float _cooldownSeconds;
        private readonly float _doneAtMeters;
        private float _cooldownLeft;
        private float _activeFor;

        /// <param name="doneAtMeters">Distance at which the retreat ends (the lower bound of the preferred band).</param>
        public RetreatPlanner(float belowMeters, float cooldownSeconds, float doneAtMeters)
        {
            _belowMeters = belowMeters;
            _cooldownSeconds = cooldownSeconds;
            _doneAtMeters = doneAtMeters > belowMeters ? doneAtMeters : belowMeters;
        }

        public bool Enabled => _belowMeters > 0f;
        public bool Active { get; private set; }
        public float CooldownLeft => _cooldownLeft;

        /// <summary>Updates the state; <paramref name="canStart"/> false (attacking, stunned) never starts a retreat.</summary>
        public bool Update(float distance, float dt, bool canStart)
        {
            if (!Enabled) return false;
            if (_cooldownLeft > 0f) _cooldownLeft = _cooldownLeft > dt ? _cooldownLeft - dt : 0f;
            if (Active)
            {
                // A cornered enemy gives up after the cooldown span instead of backing into a wall forever.
                _activeFor += dt;
                if (distance >= _doneAtMeters || _activeFor >= _cooldownSeconds) Active = false;
            }
            else if (canStart && _cooldownLeft <= 0f && distance < _belowMeters)
            {
                Active = true;
                _activeFor = 0f;
                _cooldownLeft = _cooldownSeconds;
            }
            return Active;
        }

        public void Reset()
        {
            Active = false;
            _activeFor = 0f;
            _cooldownLeft = 0f;
        }
    }
}

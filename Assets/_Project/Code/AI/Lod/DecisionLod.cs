namespace Eclipse.AI.Lod
{
    /// <summary>
    /// Per-enemy decision throttle: near (within radius) or on-screen decides at NearHz, otherwise FarHz.
    /// Call <see cref="ShouldDecide"/> once per simulation tick; the first call always decides.
    /// </summary>
    public sealed class DecisionLod
    {
        private const float Epsilon = 1e-4f;
        private readonly DecisionLodConfig _config;
        private float _accum;
        private bool _started;
        private bool _wasNear;

        public DecisionLod(DecisionLodConfig config) { _config = config; }

        public bool IsNear { get; private set; }
        public float CurrentHz { get; private set; }

        public bool ShouldDecide(float dt, float distanceToPlayer, bool onScreen)
        {
            IsNear = onScreen || distanceToPlayer <= _config.NearRadius;
            CurrentHz = IsNear ? _config.NearHz : _config.FarHz;
            if (!_started)
            {
                _started = true;
                _wasNear = IsNear;
                _accum = 0f;
                return true;
            }
            if (IsNear != _wasNear)
            {
                // Rate switch: restart the interval so the new rate applies cleanly.
                _wasNear = IsNear;
                _accum = 0f;
            }
            _accum += dt;
            if (_accum + Epsilon >= 1f / CurrentHz)
            {
                _accum = 0f;
                return true;
            }
            return false;
        }
    }
}

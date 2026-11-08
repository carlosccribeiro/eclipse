using System;

namespace Eclipse.AI.Telegraphs
{
    public enum TelegraphEvent : byte { None, Impact }

    /// <summary>
    /// Pure timeline of one telegraph (09 section 3.3, 06 section 3.10). Progress goes 0 to 1 as time over wind-up;
    /// <see cref="Advance"/> returns <see cref="TelegraphEvent.Impact"/> on the exact tick in which progress reaches 1.
    /// A Stagger/CC applied while running cancels it when the attack is interruptible (cyan ring window).
    /// </summary>
    public sealed class TelegraphTimeline
    {
        private const float Tolerance = 1e-5f;

        private float _windup;
        private float _elapsed;
        private bool _interruptible;
        private bool _running;

        public bool IsRunning => _running;
        public bool Interruptible => _interruptible;
        public float WindupSeconds => _windup;
        public float Progress => _windup <= 0f ? (_running ? 1f : 0f) : Math.Min(1f, _elapsed / _windup);

        public void Start(float windupSeconds, bool interruptible)
        {
            if (windupSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(windupSeconds));
            _windup = windupSeconds;
            _elapsed = 0f;
            _interruptible = interruptible;
            _running = true;
        }

        public TelegraphEvent Advance(float deltaSeconds)
        {
            if (!_running) return TelegraphEvent.None;
            _elapsed += deltaSeconds;
            if (_elapsed >= _windup - Tolerance)
            {
                _elapsed = _windup;
                _running = false;
                return TelegraphEvent.Impact;
            }
            return TelegraphEvent.None;
        }

        /// <summary>Stagger/CC arrived: cancels the wind-up if it is interruptible. Returns true when cancelled.</summary>
        public bool TryInterrupt()
        {
            if (!_running || !_interruptible) return false;
            _running = false;
            return true;
        }

        /// <summary>Unconditional cancel (death, hard reset).</summary>
        public void Cancel() => _running = false;
    }
}

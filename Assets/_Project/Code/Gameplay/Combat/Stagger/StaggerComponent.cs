using System;
using Eclipse.Gameplay.Simulation;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Stagger bar of one target (06 section 3.8.2, R11). Hits fill the bar; a full bar causes Staggered (stun + Vulnerability) or,
    /// on a region boss, Broken (longer, bigger Vulnerability, global hit-stop event, threshold grows after each Break).
    /// The bar decays after a delay without gains. Under Unstoppable the bar fills but the Staggered is deferred until it ends.
    /// Pure C#; time is the simulation frame set by <see cref="Tick(int)"/>. Allocation-free after construction.
    /// Assumptions (M05 log): decay is a fraction of the maximum per second; hits while Staggered/Broken do not fill the bar;
    /// an Elite with no bar tier (Light) has no Stagger.
    /// </summary>
    public sealed class StaggerComponent : IStaggerable, IFixedTickable
    {
        private readonly CombatStateRules _rules;
        private readonly EntityId _id;
        private readonly StaggerTier _tier;
        private readonly bool _isElite;
        private readonly float _difficulty;
        private readonly CombatEventStream _events;
        private readonly StateComponent _states;

        private float _current;
        private int _lastGain, _lastUpdate;
        private int _stateUntil;
        private int _breakCount;
        private bool _pending;
        private StaggerState _state;

        /// <param name="difficultyMultiplier">Threshold multiplier from <see cref="CombatStateRules.DifficultyMultiplier"/>.</param>
        /// <param name="states">Optional: provides Unstoppable (deferral) and flinch checks.</param>
        public StaggerComponent(CombatStateRules rules, EntityId id, StaggerTier tier, bool isElite,
            float difficultyMultiplier, CombatEventStream events, StateComponent states = null)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _id = id;
            _tier = tier;
            _isElite = isElite;
            _difficulty = difficultyMultiplier;
            _events = events;
            _states = states;
        }

        public EntityId Id => _id;
        public int CurrentFrame { get; private set; }
        public float StaggerCurrent => _current;
        public StaggerState State => _state;
        public bool IsStaggered => _state != StaggerState.Normal;
        public bool IsBroken => _state == StaggerState.Broken;
        public int BreakCount => _breakCount;
        /// <summary>Hard-stunned by the Stagger state: cannot act.</summary>
        public bool IsActionBlocked => IsStaggered;
        public bool HasUnstoppablePending => _pending;

        /// <summary>Current threshold: base x difficulty x (1 + growth x breaks, boss only).</summary>
        public float StaggerMax
        {
            get
            {
                float growth = 1f;
                if (_tier == StaggerTier.Boss)
                {
                    int n = _breakCount < _rules.V.BreakThresholdMaxGrowths ? _breakCount : _rules.V.BreakThresholdMaxGrowths;
                    growth += _rules.V.BreakThresholdGrowth * n;
                }
                return _rules.BaseThreshold(_tier, _isElite) * _difficulty * growth;
            }
        }

        /// <summary>Extra damage taken while Staggered (+20%) or Broken (+30%); a Vulnerability source, not More.</summary>
        public float VulnerabilityBonus =>
            _state == StaggerState.Broken ? _rules.V.BrokenVulnerability
            : _state == StaggerState.Staggered ? _rules.V.StaggeredVulnerability : 0f;

        public void Tick(in SimulationFrame frame) => Tick(frame.Index);

        public void Tick(int frame)
        {
            CurrentFrame = frame;
            if (_state != StaggerState.Normal && frame >= _stateUntil) { _state = StaggerState.Normal; _lastGain = frame; _lastUpdate = frame; }
            if (_pending && !Unstoppable(frame)) Trigger(EntityId.None, 0, frame);
            else Decay(frame);
        }

        /// <summary>Adds Stagger (already multiplied by Stagger Damage%). Returns true if this hit started a Staggered/Broken.</summary>
        public bool AddStagger(float amount, EntityId source, int generation)
        {
            int frame = CurrentFrame;
            if (amount <= 0f) return false;
            if (_states != null && _states.ShouldFlinch(amount, frame))
                _events?.Publish(new OnInterrupt(frame, generation, source, _id));
            if (_tier == StaggerTier.None) return false;

            Decay(frame);
            _events?.Publish(new OnStaggerApplied(frame, generation, source, _id, amount));
            if (_state != StaggerState.Normal || _pending) return false;

            _current += amount;
            _lastGain = frame;
            float max = StaggerMax;
            if (_current < max) return false;
            if (Unstoppable(frame)) { _current = max; _pending = true; return false; }
            Trigger(source, generation, frame);
            return true;
        }

        /// <summary>
        /// Interruptible window (cyan ring, 06 3.10): Stagger/CC applied in it causes an immediate Staggered.
        /// Does nothing under Unstoppable, on targets without a bar, or while already Staggered.
        /// </summary>
        public bool ForceStaggered(EntityId source, int generation)
        {
            int frame = CurrentFrame;
            if (_tier == StaggerTier.None || _state != StaggerState.Normal || Unstoppable(frame)) return false;
            _events?.Publish(new OnInterrupt(frame, generation, source, _id));
            Trigger(source, generation, frame);
            return true;
        }

        private bool Unstoppable(int frame) => _states != null && _states.IsUnstoppable(frame);

        private void Decay(int frame)
        {
            if (_state == StaggerState.Normal && _current > 0f && !_pending)
            {
                int from = _lastGain + CombatStateRules.Frames(_rules.V.StaggerDecayDelaySeconds);
                if (_lastUpdate > from) from = _lastUpdate;
                if (frame > from)
                {
                    _current -= StaggerMax * _rules.V.StaggerDecayFractionPerSecond * (frame - from) / SimulationFrame.TicksPerSecond;
                    if (_current < 0f) _current = 0f;
                }
            }
            _lastUpdate = frame;
        }

        private void Trigger(EntityId source, int generation, int frame)
        {
            _current = 0f;
            _pending = false;
            _lastGain = frame;
            _lastUpdate = frame;
            if (_tier == StaggerTier.Boss)
            {
                _breakCount++;
                _state = StaggerState.Broken;
                _stateUntil = frame + CombatStateRules.Frames(_rules.V.BrokenSeconds);
                _events?.Publish(new OnStaggerBreak(frame, generation, source, _id, _breakCount));
                _events?.Publish(new OnGlobalHitStop(frame, generation, _id, _rules.V.BrokenHitStopFrames));
                return;
            }
            float seconds = _tier == StaggerTier.Guardian ? _rules.V.StaggeredGuardianSeconds
                : _isElite ? _rules.V.StaggeredEliteSeconds : _rules.V.StaggeredSeconds;
            _state = StaggerState.Staggered;
            _stateUntil = frame + CombatStateRules.Frames(seconds);
            _events?.Publish(new OnStaggered(frame, generation, source, _id));
        }
    }
}

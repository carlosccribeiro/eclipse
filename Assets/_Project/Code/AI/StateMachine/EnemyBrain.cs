using Eclipse.AI.Lod;
using Eclipse.AI.Utility;
using Eclipse.Core.Random;

namespace Eclipse.AI.StateMachine
{
    /// <summary>
    /// Common-enemy brain: HFSM <c>Idle -> Engage -> [Approach | Reposition | Attack | Recover | Flee] -> Dead</c>
    /// (11 section 3.10) driven by <see cref="UtilitySelector"/> and throttled by <see cref="DecisionLod"/>.
    /// Pure C#, allocation free per tick. Call <see cref="Tick"/> every simulation step (60 Hz); new decisions
    /// happen only when the LOD allows. Timers (Attack/Recover) and death run every tick.
    /// </summary>
    public sealed class EnemyBrain
    {
        private readonly UtilityConfig _config;
        private readonly UtilitySelector _selector;
        private readonly DecisionLod _lod;
        private readonly ITokenGate _gate;
        private readonly int _ownerId;

        private float _stateTime;
        private int _attackIndex = -1;
        private bool _holdsToken;

        public EnemyBrain(UtilityConfig config, DecisionLodConfig lodConfig, RngStreams rng, ITokenGate tokenGate, int ownerId)
        {
            _config = config;
            _selector = new UtilitySelector(config, rng);
            _lod = new DecisionLod(lodConfig);
            _gate = tokenGate;
            _ownerId = ownerId;
        }

        public EnemyState State { get; private set; } = EnemyState.Idle;
        public bool IsEngaged => State != EnemyState.Idle && State != EnemyState.Dead;
        public DecisionLod Lod => _lod;

        /// <summary>Advances the brain by <paramref name="dt"/> seconds. A null token gate allows every attack.</summary>
        public AiDecision Tick(in AiPerception p, float dt)
        {
            var d = new AiDecision { State = State, PreviousState = State, AttackIndex = -1 };
            if (State == EnemyState.Dead) return d;

            if (p.IsDead)
            {
                Enter(EnemyState.Dead, ref d);
                return d;
            }

            _stateTime += dt;
            d.AttackIndex = _attackIndex;

            switch (State)
            {
                case EnemyState.Attack:
                {
                    var opt = _config.Attacks[_attackIndex];
                    if (_stateTime >= opt.DurationSeconds) Enter(EnemyState.Recover, ref d);
                    return d;
                }
                case EnemyState.Recover:
                {
                    var opt = _config.Attacks[_attackIndex];
                    if (_stateTime >= opt.RecoverSeconds)
                        Enter(p.HasTarget ? EnemyState.Engage : EnemyState.Idle, ref d);
                    return d;
                }
            }

            if (!_lod.ShouldDecide(dt, p.Distance, p.OnScreen)) return d;
            d.Evaluated = true;

            if (!p.HasTarget)
            {
                if (State != EnemyState.Idle) Enter(EnemyState.Idle, ref d);
                return d;
            }
            if (State == EnemyState.Idle)
            {
                Enter(EnemyState.Engage, ref d);
                return d;
            }

            _selector.Select(in p, true, out var action, out int attackIdx, out float score, out bool randomized);
            if (action == UtilityAction.Attack && !TryAcquire(attackIdx))
            {
                // Token denied: orbit instead (11 section 3.10).
                _selector.Select(in p, false, out action, out attackIdx, out score, out randomized);
            }

            d.Action = action;
            d.Score = score;
            d.Randomized = randomized;
            switch (action)
            {
                case UtilityAction.Approach: Enter(EnemyState.Approach, ref d); break;
                case UtilityAction.Reposition: Enter(EnemyState.Reposition, ref d); break;
                case UtilityAction.Flee: Enter(EnemyState.Flee, ref d); break;
                case UtilityAction.Attack:
                    _attackIndex = attackIdx;
                    d.AttackIndex = attackIdx;
                    d.AttackStarted = true;
                    Enter(EnemyState.Attack, ref d);
                    break;
                default: Enter(EnemyState.Engage, ref d); break;
            }
            return d;
        }

        private bool TryAcquire(int attackIdx)
        {
            if (_gate == null) { _holdsToken = false; return true; }
            if (!_gate.TryAcquire(_ownerId, _config.Attacks[attackIdx].Ranged)) return false;
            _holdsToken = true;
            return true;
        }

        private void Enter(EnemyState next, ref AiDecision d)
        {
            if (_holdsToken && next != EnemyState.Attack)
            {
                _gate.Release(_ownerId);
                _holdsToken = false;
            }
            if (next == State) return;
            d.PreviousState = State;
            d.StateChanged = true;
            State = next;
            d.State = next;
            _stateTime = 0f;
        }
    }
}

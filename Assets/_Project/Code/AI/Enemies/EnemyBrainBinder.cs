using System;
using Eclipse.AI.Director;
using Eclipse.AI.Lod;
using Eclipse.AI.StateMachine;
using Eclipse.AI.Utility;
using Eclipse.Core.Random;
using UnityEngine;

namespace Eclipse.AI.Enemies
{
    /// <summary>World snapshot for one binder tick. Filled by the controller (or by a test).</summary>
    public struct BinderInput
    {
        public bool IsDead;
        public bool HasTarget;
        public bool OnScreen;
        /// <summary>False while stunned/Staggered: no new attack starts.</summary>
        public bool CanAct;
        public Vector3 Self;
        public Vector3 Forward;
        public Vector3 Target;
        public float HpFraction;
        public int AlliesNearby;
        /// <summary>Wall line of sight to the target (06 section 3.9: only Wall blocks).</summary>
        public bool HasLineOfSight;
        /// <summary>A Tank ally stands on the straight line to the target (09 4.2: ranged do not shoot through it).</summary>
        public bool ShotBlockedByAlly;
        public bool HasEscortWard;
        public Vector3 EscortWard;
    }

    /// <summary>Result of one binder tick.</summary>
    public struct BinderOutput
    {
        public EnemyState State;
        public SteeringCommand Steering;
        /// <summary>True when the enemy may turn toward the target (not during a wind-up: the aim is locked).</summary>
        public bool FaceTarget;
        public Vector3 FaceDirection;
        /// <summary>Index into <see cref="EnemyBlueprint.Heads"/> of the attack the brain started this tick, else -1.</summary>
        public int AttackStarted;
        /// <summary>Dead and the on-death attack (if any) has finished: the controller may return the enemy to the pool.</summary>
        public bool ReadyToDespawn;
    }

    /// <summary>
    /// Glue between the pure parts (brief of T6, <c>EnemyBrainBinder</c>): feeds <see cref="EnemyBrain"/> an
    /// <see cref="AiPerception"/> (distance, angle, cooldowns, allies, HP) at the LOD rate, runs the attack the decision picked
    /// through an <see cref="AttackExecutor"/>, plans the steering and handles death (on-death attacks, token release).
    /// Pure C#, no allocation per tick; RNG only from the Combat stream. Cooldowns are taken from the attack data and start when
    /// the brain starts the attack.
    /// </summary>
    public sealed class EnemyBrainBinder
    {
        /// <summary>Marks an attack as unavailable in the perception without being a data number (any value &gt; 0 means "not ready").</summary>
        private const float GateMarker = 1e-3f;

        private readonly EnemyBlueprint _blueprint;
        private readonly TokenProbe _gate;
        private readonly RngStreams _rng;
        private readonly int _ownerId;
        private readonly DecisionLodConfig _lod;
        private readonly TokenOrbitConfig _orbit;
        private readonly int _slot;
        private readonly AttackExecutor _executor;
        private readonly RetreatPlanner _retreat;
        private readonly float[] _cooldowns;
        private readonly float[] _view;

        private EnemyBrain _brain;
        private EnemyState _moveState = EnemyState.Approach;
        private bool _deathHandled;
        private bool _orbiting;

        /// <summary>Remembers a denied token so the enemy orbits instead of idling in range (09 section 3.2).</summary>
        private sealed class TokenProbe : ITokenGate
        {
            private readonly ITokenGate _inner;
            public bool Denied;
            public TokenProbe(ITokenGate inner) { _inner = inner; }
            public bool TryAcquire(int ownerId, bool ranged)
            {
                bool ok = _inner.TryAcquire(ownerId, ranged);
                if (!ok) Denied = true;
                return ok;
            }
            public void Release(int ownerId) => _inner.Release(ownerId);
        }

        public EnemyBrainBinder(EnemyBlueprint blueprint, ITokenGate gate, RngStreams rng, int ownerId, DecisionLodConfig lod,
            TokenOrbitConfig orbit, IAttackDelivery delivery)
        {
            _blueprint = blueprint ?? throw new ArgumentNullException(nameof(blueprint));
            _gate = gate != null ? new TokenProbe(gate) : null;
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _ownerId = ownerId;
            _lod = lod ?? throw new ArgumentNullException(nameof(lod));
            _orbit = orbit;
            _slot = ownerId;
            _executor = new AttackExecutor(delivery ?? throw new ArgumentNullException(nameof(delivery)));
            var def = blueprint.Definition;
            _retreat = blueprint.Template.UsesRetreat
                ? new RetreatPlanner(def.RetreatBelowMeters, def.RetreatCooldownSeconds, def.PreferredRangeMin)
                : new RetreatPlanner(0f, 0f, 0f);
            _cooldowns = new float[blueprint.Heads.Length];
            _view = new float[blueprint.Heads.Length];
            _brain = NewBrain();
        }

        public EnemyState State => _brain.State;
        public AttackExecutor Executor => _executor;
        public RetreatPlanner Retreat => _retreat;
        public EnemyBlueprint Blueprint => _blueprint;
        public EnemyBrain Brain => _brain;
        public float CooldownRemaining(int headIndex) => _cooldowns[headIndex];

        /// <summary>Back to a fresh Idle enemy (pool reuse). Releases nothing: the controller unregisters from the director.</summary>
        public void Reset()
        {
            _executor.Cancel();
            _brain = NewBrain();
            _retreat.Reset();
            _moveState = EnemyState.Approach;
            _deathHandled = false;
            _orbiting = false;
            Array.Clear(_cooldowns, 0, _cooldowns.Length);
        }

        /// <summary>Stagger damage arrived: cancels an interruptible wind-up (cyan ring, 06 section 3.10). True when cancelled.</summary>
        public bool TryInterrupt() => _executor.TryInterrupt();

        /// <summary>Stun/Staggered: whatever is running stops.</summary>
        public void CancelAttack() => _executor.Cancel();

        private EnemyBrain NewBrain() => new EnemyBrain(_blueprint.Utility, _lod, _rng, _gate, _ownerId);

        public void Tick(in BinderInput input, float dt, ref BinderOutput output)
        {
            for (int i = 0; i < _cooldowns.Length; i++)
                _cooldowns[i] = _cooldowns[i] > dt ? _cooldowns[i] - dt : 0f;

            Vector3 toTarget = input.Target - input.Self;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            Vector3 flatForward = input.Forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 1e-6f) flatForward = Vector3.forward;
            flatForward.Normalize();
            Vector3 dirToTarget = distance < 1e-4f ? flatForward : toTarget / distance;
            float angle = Vector3.Angle(flatForward, dirToTarget);
            var current = new AttackAim { Origin = input.Self, Forward = dirToTarget, Target = input.Target };

            output.AttackStarted = -1;
            output.FaceTarget = false;
            output.FaceDirection = dirToTarget;
            output.ReadyToDespawn = false;

            if (input.IsDead)
            {
                TickDead(in input, dt, in current, ref output);
                return;
            }

            if (_executor.IsBusy) _executor.Tick(dt, in current);

            bool canRetreatNow = input.HasTarget && input.CanAct && !_executor.IsBusy
                && _brain.State != EnemyState.Attack && _brain.State != EnemyState.Recover;
            bool retreating = _retreat.Update(distance, dt, canRetreatNow);

            for (int i = 0; i < _view.Length; i++)
            {
                AttackPlan plan = _blueprint.Heads[i];
                float v = _cooldowns[i];
                if (v <= 0f && plan.IsProjectileLike
                    && (!input.OnScreen || !input.HasLineOfSight || (plan.IsStraightProjectile && input.ShotBlockedByAlly)))
                    v = GateMarker;
                _view[i] = v;
            }

            var perception = new AiPerception
            {
                IsDead = false,
                HasTarget = input.HasTarget,
                OnScreen = input.OnScreen,
                Distance = distance,
                AngleDeg = angle,
                HpFraction = input.HpFraction,
                AlliesNearby = input.AlliesNearby,
                CooldownsRemaining = _view,
            };
            if (_gate != null) _gate.Denied = false;
            AiDecision decision = _brain.Tick(in perception, dt);
            if (decision.Evaluated) _orbiting = _gate != null && _gate.Denied && decision.Action != UtilityAction.Attack;

            if (decision.StateChanged && (decision.State == EnemyState.Approach || decision.State == EnemyState.Reposition
                || decision.State == EnemyState.Flee))
                _moveState = decision.State;

            if (decision.AttackStarted)
            {
                output.AttackStarted = decision.AttackIndex;
                AttackPlan plan = _blueprint.Heads[decision.AttackIndex];
                _cooldowns[decision.AttackIndex] = plan.CooldownSeconds;
                bool offScreenShot = plan.IsProjectileLike && !input.OnScreen; // 06 section 3.10: no projectiles off-screen
                if (input.CanAct && !offScreenShot)
                {
                    var aim = new AttackAim { Origin = input.Self, Forward = dirToTarget, Target = input.Target };
                    _executor.Begin(plan, in aim);
                }
            }

            output.State = _brain.State;
            output.FaceTarget = input.HasTarget && !_executor.IsBusy && _brain.State != EnemyState.Attack;
            output.Steering = BuildSteering(in input, retreating);
        }

        private SteeringCommand BuildSteering(in BinderInput input, bool retreating)
        {
            if (!input.HasTarget) return SteeringCommand.Hold;
            if (_executor.IsBusy) return SteeringCommand.Hold;
            var def = _blueprint.Definition;
            var ctx = new SteeringContext
            {
                Self = input.Self,
                Target = input.Target,
                Template = _blueprint.Template,
                PreferredMin = def.PreferredRangeMin,
                PreferredMax = def.PreferredRangeMax,
                OrbitMin = _orbit.MinMeters,
                OrbitMax = _orbit.MaxMeters,
                Slot = _slot,
                HasLineOfSight = input.HasLineOfSight,
                HasEscortWard = input.HasEscortWard,
                EscortWard = input.EscortWard,
                Retreating = retreating,
            };
            // No token and nothing better to do: orbit the target (09 section 3.2).
            EnemyState state = _orbiting && _brain.State == EnemyState.Engage ? EnemyState.Reposition : _brain.State;
            return SteeringPlanner.Plan(state, _moveState, in ctx);
        }

        private void TickDead(in BinderInput input, float dt, in AttackAim current, ref BinderOutput output)
        {
            if (!_deathHandled)
            {
                _deathHandled = true;
                _executor.Cancel();
                var perception = new AiPerception { IsDead = true };
                _brain.Tick(in perception, dt); // enters Dead and returns the attack token
                RollDeathAttack(in input);
            }
            else if (_executor.IsBusy)
            {
                _executor.Tick(dt, in current);
            }
            output.State = EnemyState.Dead;
            output.Steering = SteeringCommand.Hold;
            output.ReadyToDespawn = !_executor.IsBusy;
        }

        // One Combat-stream roll per on-death attack, in data order; the first success fires.
        private void RollDeathAttack(in BinderInput input)
        {
            AttackPlan[] plans = _blueprint.DeathPlans;
            for (int i = 0; i < plans.Length; i++)
            {
                ref Xoshiro128StarStar gen = ref _rng.Get(RngStream.Combat);
                float roll = gen.NextFloat01();
                if (roll >= plans[i].TriggerChance) continue;
                Vector3 forward = input.Forward;
                forward.y = 0f;
                forward = forward.sqrMagnitude < 1e-6f ? Vector3.forward : forward.normalized;
                var aim = new AttackAim { Origin = input.Self, Forward = forward, Target = input.Self };
                _executor.Begin(plans[i], in aim);
                return;
            }
        }
    }
}

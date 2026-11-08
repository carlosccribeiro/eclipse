using System;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Attacks;
using Eclipse.Gameplay.Heroes;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>Adapter: <see cref="StateComponent"/> as <see cref="ISkillState"/>.</summary>
    public sealed class StateComponentSkillState : ISkillState
    {
        private readonly StateComponent _state;

        public StateComponentSkillState(StateComponent state) { _state = state ?? throw new ArgumentNullException(nameof(state)); }

        public bool IsStunned(int frame) => _state.IsStunned(frame);
        public bool IsFrozen(int frame) => _state.IsFrozen(frame);
        public bool IsSilenced(int frame) => _state.IsSilenced(frame);
        public bool IsRooted(int frame) => _state.IsRooted(frame);
    }

    /// <summary>Adapter: <see cref="ResourcePool"/> as <see cref="ISkillResource"/>. Gains go through a multiplier supplier (Resource Generation).</summary>
    public sealed class ResourcePoolSkillResource : ISkillResource
    {
        private readonly ResourcePool _pool;
        private readonly Func<float> _generationMultiplier;

        /// <param name="publishesSpentEvent">True when the pool has a sink that publishes OnResourceSpent (ResourceComponent).</param>
        public ResourcePoolSkillResource(ResourcePool pool, bool publishesSpentEvent, Func<float> generationMultiplier = null)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            PublishesSpentEvent = publishesSpentEvent;
            _generationMultiplier = generationMultiplier;
        }

        public float Current => _pool.Current;
        public bool PublishesSpentEvent { get; }
        public bool TrySpend(float amount) => _pool.TrySpend(amount);
        public float SpendAll() => _pool.SpendAll();
        public void Gain(float amount) => _pool.Add(amount * (_generationMultiplier == null ? 1f : _generationMultiplier()));
    }

    /// <summary>Adapter: <see cref="AttackController"/> (M05) as <see cref="IAttackCancelHost"/>.</summary>
    public sealed class AttackControllerCancelHost : IAttackCancelHost
    {
        private readonly AttackController _attacks;

        public AttackControllerCancelHost(AttackController attacks) { _attacks = attacks ?? throw new ArgumentNullException(nameof(attacks)); }

        public bool CanCancelInto(ActionKind to) => _attacks.CanCancelInto(to);
        public void CancelForAction() => _attacks.CancelForAction();
    }
}

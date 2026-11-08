using System;
using Eclipse.Core.Ids;
using Eclipse.Data.Heroes;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Simulation;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Gameplay.Heroes
{
    /// <summary>
    /// Thin MonoBehaviour around a <see cref="ResourcePool"/>: ticks it at 60 Hz and publishes OnResourceGained/Spent/Full/Empty
    /// on the <see cref="CombatEventStream"/> (06 3.11, 11 3.6.4). Hero rules live in pure classes (e.g. KaelFury) that wrap <see cref="Pool"/>.
    /// Register in the TickScheduler (after combat) from the hero setup.
    /// </summary>
    public sealed class ResourceComponent : MonoBehaviour, IFixedTickable
    {
        private sealed class Sink : IResourceSink
        {
            public ResourceComponent Owner;
            public void Gained(float amount) => Owner._events?.Publish(new OnResourceGained(Owner._frame, 0, Owner._ownerId, Owner._resourceId, amount));
            public void Spent(float amount) => Owner._events?.Publish(new OnResourceSpent(Owner._frame, 0, Owner._ownerId, Owner._resourceId, amount));
            public void Full() => Owner._events?.Publish(new OnResourceFull(Owner._frame, 0, Owner._ownerId, Owner._resourceId));
            public void Empty() => Owner._events?.Publish(new OnResourceEmpty(Owner._frame, 0, Owner._ownerId, Owner._resourceId));
        }

        [SerializeField] private ResourceDefinition definition;

        private CombatEventStream _events;
        private EntityId _ownerId;
        private StableId _resourceId;
        private int _frame;

        public ResourcePool Pool { get; private set; }
        public ResourceDefinition Definition => definition;

        /// <summary>Raised at the end of each tick, after the pool tick (hook for hero logic such as passives).</summary>
        public event Action<ResourceComponent> Ticked;

        public void Initialize(ResourceDefinition def, EntityId owner, CombatEventStream events)
        {
            definition = def != null ? def : throw new ArgumentNullException(nameof(def));
            _ownerId = owner;
            _events = events;
            _resourceId = StableId.Parse(def.StableIdString);
            Pool = new ResourcePool(ResourceRules.FromDefinition(def), new Sink { Owner = this });
        }

        /// <summary>Frame stamped on events published outside <see cref="Tick"/> (hit handlers).</summary>
        public void SetFrame(int frame) => _frame = frame;

        public void Tick(in SimulationFrame frame)
        {
            if (Pool == null) return;
            _frame = frame.Index;
            Pool.Tick(frame.DeltaSeconds);
            Ticked?.Invoke(this);
        }
    }
}

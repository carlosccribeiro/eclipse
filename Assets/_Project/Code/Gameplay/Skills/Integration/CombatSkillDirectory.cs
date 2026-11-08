using System.Collections.Generic;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Simulation;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>
    /// <see cref="ISkillTargetDirectory"/> over the <see cref="CombatWorld"/> registry: builds the full set of handles of a combatant
    /// the first time a skill reaches it and caches it (runtime ids are never reused inside a scene). Non-player actors get the
    /// provisional Burning <see cref="StatusStub"/> (M05; M11 replaces it behind <see cref="IStatusReceiver"/>) and a vulnerability set.
    /// Armor% and the Stagger vulnerability are refreshed on every lookup because they change while the target lives.
    /// </summary>
    public sealed class CombatSkillDirectory : ISkillTargetDirectory
    {
        private readonly CombatWorld _world;
        private readonly Dictionary<int, SkillTarget> _cache = new Dictionary<int, SkillTarget>(32);

        public CombatSkillDirectory(CombatWorld world)
        {
            _world = world;
        }

        /// <summary>Registers an externally built target (the caster).</summary>
        public void Add(SkillTarget target) => _cache[target.Id.Value] = target;

        public bool TryGet(EntityId id, out SkillTarget target)
        {
            if (_cache.TryGetValue(id.Value, out target))
            {
                Refresh(target, id);
                return true;
            }
            if (!_world.TryGetActor(id, out CombatActor actor) || actor.Entity == null)
            {
                target = null;
                return false;
            }
            target = new SkillTarget
            {
                Id = id,
                Entity = actor.Entity,
                Receiver = actor.Receiver,
                State = actor.States,
                Stagger = actor.Stagger,
                Shield = actor.Entity.Shield,
                Timed = actor.Timed,
                Vulnerability = new TimedVulnerabilitySet(),
            };
            if (!actor.IsPlayer)
            {
                var status = new StatusStub(id, new[] { StatusStubDefinition.BurningProvisional() }, _world.Events);
                _world.Services.Scheduler.Register(status);
                target.Status = status;
            }
            _cache[id.Value] = target;
            Refresh(target, id);
            return true;
        }

        private void Refresh(SkillTarget target, EntityId id)
        {
            if (!_world.TryGetActor(id, out CombatActor actor) || actor.IsPlayer) return;
            target.ArmorPercent = actor.ArmorPercent;
            target.ExternalVulnerability = actor.Stagger != null ? actor.Stagger.VulnerabilityBonus : 0f;
        }
    }
}

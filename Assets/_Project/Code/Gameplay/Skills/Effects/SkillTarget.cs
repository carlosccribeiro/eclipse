using Eclipse.Gameplay.Combat;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>
    /// Handles of one combatant as seen by Effects (caster or victim). Every handle is optional: an Effect that needs a missing
    /// one does nothing. Built by the combat registry (<see cref="ISkillTargetDirectory"/>); tests fill only what they exercise.
    /// </summary>
    public sealed class SkillTarget
    {
        public EntityId Id;
        public ICombatEntity Entity;
        public IDamageReceiver Receiver;
        public IStatusReceiver Status;
        public StateComponent State;
        public IStaggerable Stagger;
        public ShieldComponent Shield;
        public TimedModifierSet Timed;
        public TimedVulnerabilitySet Vulnerability;
        /// <summary>Armor% of a non-player target before Armor Penetration (01 3.4.2).</summary>
        public float ArmorPercent;
        /// <summary>Vulnerability that lives outside <see cref="Vulnerability"/> (Staggered/Broken of the Stagger bar), summed into the hit.</summary>
        public float ExternalVulnerability;
        /// <summary>Used only when <see cref="Entity"/> is null (position reported by the area query).</summary>
        public Vector3 FallbackPosition;

        public Vector3 Position => Entity != null ? Entity.Position : FallbackPosition;
        public bool IsAlive => Entity == null || Entity.IsAlive;

        public void Clear()
        {
            Id = default; Entity = null; Receiver = null; Status = null; State = null; Stagger = null; Shield = null;
            Timed = null; Vulnerability = null; ArmorPercent = 0f; ExternalVulnerability = 0f; FallbackPosition = default;
        }
    }

    /// <summary>Resolves a runtime <see cref="EntityId"/> to the full set of handles (spawner / combat registry implements it).</summary>
    public interface ISkillTargetDirectory
    {
        bool TryGet(EntityId id, out SkillTarget target);
    }

    /// <summary>Aim and displacement of the caster (the thin player MonoBehaviour implements it; tests fake it).</summary>
    public interface ISkillCasterMotion
    {
        Vector3 Forward { get; }
        /// <summary>Aimed ground point (cursor), planar.</summary>
        Vector3 AimPoint { get; }
        /// <summary>Teleports the caster to a pose produced by a Delivery (Leap).</summary>
        void SetPosition(Vector3 position);
    }
}

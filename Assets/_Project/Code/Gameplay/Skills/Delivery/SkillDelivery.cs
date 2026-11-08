using Eclipse.Data.Skills;
using UnityEngine;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>
    /// Runtime Delivery (11 3.6.1): how the skill reaches its targets. Shared, immutable asset data; any per-cast state lives in the
    /// <see cref="SkillExecContext"/>. The <see cref="SkillRunner"/> forwards the controller phases here. Shapes and ranges are read from
    /// the skill's params by key (CSV), so the same Delivery serves every hero.
    /// </summary>
    [System.Serializable]
    public abstract class SkillDelivery : Delivery
    {
        /// <summary>Phase in which a one-shot Delivery connects (Active by default: the hit frames of 06 3.6).</summary>
        [SerializeField] protected SkillPhase impactPhase = SkillPhase.Active;

        public SkillPhase ImpactPhase => impactPhase;

        /// <summary>Code-assembled skills: moves the connect point to another phase. Returns this.</summary>
        public SkillDelivery WithImpactPhase(SkillPhase phase)
        {
            impactPhase = phase;
            return this;
        }

        /// <summary>Called on every announced phase of the cast, after the ActiveStart effects when entering Active.</summary>
        public virtual void OnPhaseEnter(SkillExecContext ctx, SkillPhase phase) { }

        /// <summary>Called once per simulation frame while the cast runs (movement, stance).</summary>
        public virtual void Tick(SkillExecContext ctx) { }

        /// <summary>The cast ended (<paramref name="cancelled"/> = dodge cancel or interruption).</summary>
        public virtual void OnCastEnd(SkillExecContext ctx, bool cancelled) { }

        /// <summary>Planar unit vector rotated around Y by <paramref name="degrees"/>.</summary>
        protected static Vector3 Rotate(Vector3 forward, float degrees)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-8f) forward = Vector3.forward;
            return Quaternion.AngleAxis(degrees, Vector3.up) * forward.normalized;
        }
    }
}

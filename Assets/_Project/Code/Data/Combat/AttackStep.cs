using System;
using UnityEngine;

namespace Eclipse.Data.Combat
{
    /// <summary>Weight class of a weapon (06 section 3.6.1). Drives cancel rules, flinch and hit-stop.</summary>
    public enum WeaponWeightClass : byte
    {
        Heavy = 0,
        Medium = 1,
        Light = 2,
        Ranged = 3,
        Caster = 4,
    }

    /// <summary>Which input produces the attack.</summary>
    public enum AttackKind : byte
    {
        Basic = 0,
        Heavy = 1,
        Dash = 2,
    }

    /// <summary>
    /// Hitbox shape as authored in data (06 section 3.9). Data-side enum: Eclipse.Data cannot reference Gameplay
    /// (Core, Data, Gameplay), so the combat runtime maps this to its own HitboxKind.
    /// </summary>
    public enum AttackHitShape : byte
    {
        /// <summary>No hitbox defined by the docs (the runtime must not hit anything).</summary>
        None = 0,
        Sector = 1,
        Box = 2,
        Capsule = 3,
        Circle = 4,
        Cone = 5,
    }

    /// <summary>
    /// One attack of a chain: frame data at 60 fps / Attack Speed 1.0 (06 section 3.5), hitbox, Stagger, hit-stop,
    /// Proc Coefficient, damage multiplier (08), lunge and flags. Pure data; values come from DataSource/AttackSteps.csv.
    /// </summary>
    [Serializable]
    public struct AttackStep
    {
        [SerializeField] private int startupFrames;
        [SerializeField] private int activeFrames;
        [SerializeField] private int recoveryFrames;
        [SerializeField] private AttackHitShape shape;
        [SerializeField] private float radius;
        [SerializeField] private float arcDegrees;
        [SerializeField] private float length;
        [SerializeField] private float width;
        [SerializeField] private Vector3 offset;
        [SerializeField] private float stagger;
        [SerializeField] private float staggerMax;
        [SerializeField] private int hitStopFrames;
        [SerializeField] private float procCoefficient;
        [SerializeField] private float damageMultiplier;
        [SerializeField] private float damageMultiplierMax;
        [SerializeField] private float lungeMinMeters;
        [SerializeField] private float lungeMaxMeters;
        [SerializeField] private float lungeConeDegrees;
        [SerializeField] private int hitGroup;
        [SerializeField] private int rehitIntervalFrames;
        [SerializeField] private bool tracking;
        [SerializeField] private bool commit;
        [SerializeField] private int chargeMinFrames;
        [SerializeField] private int chargeMaxFrames;

        public int StartupFrames => startupFrames;
        public int ActiveFrames => activeFrames;
        public int RecoveryFrames => recoveryFrames;
        public int TotalFrames => startupFrames + activeFrames + recoveryFrames;
        public AttackHitShape Shape => shape;
        /// <summary>Sector/Circle/Cone/Capsule radius in meters.</summary>
        public float Radius => radius;
        /// <summary>Sector/Cone full arc in degrees (0 when the docs do not define it).</summary>
        public float ArcDegrees => arcDegrees;
        /// <summary>Box/Capsule length along forward.</summary>
        public float Length => length;
        /// <summary>Box width across.</summary>
        public float Width => width;
        /// <summary>Local offset from the attacker origin (x right, y up, z forward).</summary>
        public Vector3 Offset => offset;
        /// <summary>Stagger dealt (uncharged value for charged attacks).</summary>
        public float Stagger => stagger;
        /// <summary>Stagger at full charge; equals <see cref="Stagger"/> when the attack does not charge.</summary>
        public float StaggerMax => staggerMax;
        public int HitStopFrames => hitStopFrames;
        public float ProcCoefficient => procCoefficient;
        /// <summary>Damage multiplier as a fraction (1.10 = 110%) of the damage per hit (08 section 4.3).</summary>
        public float DamageMultiplier => damageMultiplier;
        /// <summary>Multiplier at full charge; equals <see cref="DamageMultiplier"/> when the attack does not charge.</summary>
        public float DamageMultiplierMax => damageMultiplierMax;
        public float LungeMinMeters => lungeMinMeters;
        public float LungeMaxMeters => lungeMaxMeters;
        public float LungeConeDegrees => lungeConeDegrees;
        public int HitGroup => hitGroup;
        /// <summary>0 = one hit per target per use; otherwise frames between re-hits on the same target.</summary>
        public int RehitIntervalFrames => rehitIntervalFrames;
        public bool Tracking => tracking;
        public bool Commit => commit;
        /// <summary>Minimum hold (tap) in frames; 0 when the attack does not charge.</summary>
        public int ChargeMinFrames => chargeMinFrames;
        /// <summary>Maximum charge in frames; 0 when the attack does not charge.</summary>
        public int ChargeMaxFrames => chargeMaxFrames;
        public bool IsChargeable => chargeMaxFrames > 0;

        public AttackStep(int startup, int active, int recovery, AttackHitShape hitShape, float shapeRadius, float arc,
            float shapeLength, float shapeWidth, Vector3 shapeOffset, float staggerValue, float staggerAtMax, int hitStop,
            float proc, float damageMult, float damageMultAtMax, float lungeMin, float lungeMax, float lungeCone, int group,
            int rehitFrames, bool isTracking, bool isCommit, int chargeMin, int chargeMax)
        {
            startupFrames = startup;
            activeFrames = active;
            recoveryFrames = recovery;
            shape = hitShape;
            radius = shapeRadius;
            arcDegrees = arc;
            length = shapeLength;
            width = shapeWidth;
            offset = shapeOffset;
            stagger = staggerValue;
            staggerMax = staggerAtMax;
            hitStopFrames = hitStop;
            procCoefficient = proc;
            damageMultiplier = damageMult;
            damageMultiplierMax = damageMultAtMax;
            lungeMinMeters = lungeMin;
            lungeMaxMeters = lungeMax;
            lungeConeDegrees = lungeCone;
            hitGroup = group;
            rehitIntervalFrames = rehitFrames;
            tracking = isTracking;
            commit = isCommit;
            chargeMinFrames = chargeMin;
            chargeMaxFrames = chargeMax;
        }
    }
}

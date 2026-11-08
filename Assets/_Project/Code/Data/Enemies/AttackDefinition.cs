using Eclipse.Core.Tags;
using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Enemies
{
    /// <summary>
    /// One enemy attack/action (09 sections 3.1, 3.3; 06 section 3.10). Wind-up is in seconds at Normal; the
    /// difficulty reduction and the absolute floor are applied at runtime. Frames are 60 Hz simulation frames.
    /// </summary>
    public sealed class AttackDefinition : Definition
    {
        [SerializeField] private AttackTrigger trigger;
        [SerializeField] private AttackDelivery delivery;
        [SerializeField] private float cooldownSeconds;
        [SerializeField] private float rangeMeters;
        [SerializeField] private float weight;
        [SerializeField] private string condition;
        [SerializeField] private TelegraphType telegraphType;
        [SerializeField] private TelegraphShape telegraphShape;
        [SerializeField] private float radiusMeters;
        [SerializeField] private float arcDegrees;
        [SerializeField] private float lengthMeters;
        [SerializeField] private float widthMeters;
        [SerializeField] private float innerRadiusMeters;
        [SerializeField] private float windupSeconds;
        [SerializeField] private int activeFrames;
        [SerializeField] private int recoveryFrames;
        [SerializeField] private float damageRel;
        [SerializeField] private float stagger;
        [SerializeField] private HitFlags hitFlags;
        [SerializeField] private int tokenCost;
        [SerializeField] private TokenKind tokenKind;
        [SerializeField] private bool interruptible;
        [SerializeField] private float triggerChance = 1f;
        [SerializeField] private float projectileSpeed;
        [SerializeField] private string zoneStatus;
        [SerializeField] private float zoneDurationSeconds;
        [SerializeField] private AttackDefinition chainNext;

        public AttackTrigger Trigger => trigger;
        public AttackDelivery Delivery => delivery;
        public float CooldownSeconds => cooldownSeconds;
        public float RangeMeters => rangeMeters;
        /// <summary>Utility weight of the action (09 section 3.1).</summary>
        public float Weight => weight;
        /// <summary>Availability condition, free text read by the utility selector (e.g. <c>target_dist&lt;=2.5</c>).</summary>
        public string Condition => condition;
        public TelegraphType TelegraphType => telegraphType;
        public TelegraphShape TelegraphShape => telegraphShape;
        public float RadiusMeters => radiusMeters;
        public float ArcDegrees => arcDegrees;
        public float LengthMeters => lengthMeters;
        public float WidthMeters => widthMeters;
        public float InnerRadiusMeters => innerRadiusMeters;
        public float WindupSeconds => windupSeconds;
        public int ActiveFrames => activeFrames;
        public int RecoveryFrames => recoveryFrames;
        /// <summary>Damage relative to the enemy's own damage (curve x damage multiplier).</summary>
        public float DamageRel => damageRel;
        public float Stagger => stagger;
        public HitFlags HitFlags => hitFlags;
        public int TokenCost => tokenCost;
        public TokenKind TokenKind => tokenKind;
        public bool Interruptible => interruptible;
        /// <summary>Probability fraction of the trigger (1 = always); used by on-death sparks.</summary>
        public float TriggerChance => triggerChance;
        public float ProjectileSpeed => projectileSpeed;
        /// <summary>Status id left by a ground zone (empty = none). Status definitions arrive in M11.</summary>
        public string ZoneStatus => zoneStatus;
        public float ZoneDurationSeconds => zoneDurationSeconds;
        /// <summary>Attack that follows immediately in a combo (null = end of chain).</summary>
        public AttackDefinition ChainNext => chainNext;

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSetScalars(AttackTrigger triggerValue, AttackDelivery deliveryValue, float cooldown, float range,
            float weightValue, string conditionValue, TelegraphType type, TelegraphShape shape, float radius, float arc,
            float length, float width, float inner, float windup, int active, int recovery, float damage, float staggerValue,
            HitFlags flags, int cost, TokenKind kind, bool canInterrupt, float chance, float speed, string zone, float zoneDuration)
        {
            trigger = triggerValue; delivery = deliveryValue; cooldownSeconds = cooldown; rangeMeters = range;
            weight = weightValue; condition = conditionValue; telegraphType = type; telegraphShape = shape;
            radiusMeters = radius; arcDegrees = arc; lengthMeters = length; widthMeters = width; innerRadiusMeters = inner;
            windupSeconds = windup; activeFrames = active; recoveryFrames = recovery; damageRel = damage; stagger = staggerValue;
            hitFlags = flags; tokenCost = cost; tokenKind = kind; interruptible = canInterrupt; triggerChance = chance;
            projectileSpeed = speed; zoneStatus = zone; zoneDurationSeconds = zoneDuration;
        }

        /// <summary>Editor/importer only. Returns true if the reference changed.</summary>
        public bool EditorSetChainNext(AttackDefinition next)
        {
            if (chainNext == next) return false;
            chainNext = next;
            return true;
        }
#endif
    }
}

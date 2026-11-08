using System;
using Eclipse.Gameplay.Physics;
using UnityEngine;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Hurtbox of a character: a trigger capsule on the Hurtbox_Player/Hurtbox_Enemy layer (06 3.9). The player's
    /// effective radius is 15% smaller than the visual silhouette (fairness); enemies use the silhouette.
    /// Thin scene shell: the entity is bound by the spawner via <see cref="Bind"/>.
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class Hurtbox : MonoBehaviour
    {
        /// <summary>Player hurtbox radius = silhouette x 0.85 (06 3.9, "15% menor"). Structural contract constant.</summary>
        public const float PlayerRadiusFactor = 0.85f;

        [SerializeField] private float silhouetteRadius = 0.5f;
        [SerializeField] private float height = 1.8f;
        [SerializeField] private bool isPlayer;

        private ICombatEntity _entity;
        private IDamageReceiver _receiver;

        public ICombatEntity Entity => _entity;
        public IDamageReceiver Receiver => _receiver;
        public float Height => height;
        public bool IsPlayer => isPlayer;
        public float EffectiveRadius => ComputeRadius(silhouetteRadius, isPlayer);
        public Vector3 Center => transform.position + Vector3.up * (height * 0.5f);

        /// <summary>Pure rule: effective hurtbox radius from the silhouette radius.</summary>
        public static float ComputeRadius(float silhouette, bool player) => player ? silhouette * PlayerRadiusFactor : silhouette;

        public void Configure(float silhouette, float capsuleHeight, bool player)
        {
            silhouetteRadius = silhouette;
            height = capsuleHeight;
            isPlayer = player;
            SyncCollider();
        }

        public void Bind(ICombatEntity entity, IDamageReceiver receiver)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
            _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
        }

        private void Awake() => SyncCollider();

        private void SyncCollider()
        {
            gameObject.layer = isPlayer ? PhysicsLayers.HurtboxPlayer : PhysicsLayers.HurtboxEnemy;
            var col = GetComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.direction = 1;
            col.radius = EffectiveRadius;
            col.height = Mathf.Max(height, EffectiveRadius * 2f);
            col.center = new Vector3(0f, height * 0.5f, 0f);
        }
    }
}

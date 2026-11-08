using Eclipse.Data.Combat;
using Eclipse.Gameplay.Combat.Attacks;
using Eclipse.Gameplay.Player;
using UnityEngine;

namespace Eclipse.Gameplay.Combat.Integration
{
    /// <summary>
    /// Wires the hero's attack side (M05 T10): <see cref="HitboxEvaluator"/> with the weapon resolver into the
    /// <see cref="AttackController"/>, using the entity, states and dodge built by <see cref="CombatActor"/>.
    /// The weapon comes from the data pipeline (WeaponDefinition); the Attack Speed cap is a field of the controller.
    /// </summary>
    [DefaultExecutionOrder(-700)]
    public sealed class PlayerCombatRig : MonoBehaviour
    {
        [SerializeField] private CombatActor actor;
        [SerializeField] private AttackController attack;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private DodgeController dodge;
        [SerializeField] private WeaponDefinition weapon;

        public HitboxEvaluator Evaluator { get; private set; }
        public WeaponDefinition Weapon => weapon;

        private void Start()
        {
            var world = CombatWorld.Current;
            if (world == null || actor == null || actor.Entity == null || weapon == null)
            {
                Debug.LogError("PlayerCombatRig: world/actor/weapon missing.", this);
                enabled = false;
                return;
            }
            Evaluator = new HitboxEvaluator(new WeaponHitResolver(world, weapon, attack));
            attack.Initialize(world.Services.Scheduler, world.Services.InputReader, motor, actor.Entity, Evaluator, weapon, actor.States, dodge);
        }
    }
}

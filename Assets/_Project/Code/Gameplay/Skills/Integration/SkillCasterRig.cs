using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Attacks;
using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Input;
using Eclipse.Gameplay.Player;
using Eclipse.Gameplay.Simulation;
using UnityEngine;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>
    /// Thin MonoBehaviour that drives the pure skill cycle (M08 T3/T4) once per simulation tick: skill spec recompilation,
    /// <see cref="SkillController"/>, <see cref="SkillRunner"/>, input consumption and the movement lock of the running cast.
    /// A hero rig calls <see cref="Setup"/> and then registers the specs/programs of its skills. No rule lives here.
    /// Tick order: after the motor (-100) and before the AttackController (-95), so a skill press is taken from the input
    /// buffer before the basic attack sees it, and Basic/Heavy presses are discarded while a skill runs (Skill never cancels
    /// into Basic/Heavy, 06 3.5).
    /// </summary>
    [DefaultExecutionOrder(-600)]
    public sealed class SkillCasterRig : MonoBehaviour, IFixedTickable
    {
        public const int TickPriority = -98;

        private sealed class MotorMotion : ISkillCasterMotion
        {
            private readonly PlayerMotor _motor;
            private readonly CombatEntity _entity;

            public MotorMotion(PlayerMotor motor, CombatEntity entity)
            {
                _motor = motor;
                _entity = entity;
            }

            public Vector3 Forward => _motor.Facing;
            public Vector3 AimPoint => _motor.Aim != null ? _motor.Aim.AimPoint : _entity.Position + _motor.Facing;

            public void SetPosition(Vector3 position)
            {
                _motor.Teleport(position);
                _entity.Position = position;
            }
        }

        private CombatWorld _world;
        private CombatActor _actor;
        private PlayerMotor _motor;
        private AttackController _attack;
        private bool _moveLocked;
        private bool _registered;

        public SkillController Controller { get; private set; }
        public SkillRunner Runner { get; private set; }
        public SkillExecContext Context { get; private set; }
        public SkillSpecCache Specs { get; private set; }
        public SkillTarget Caster { get; private set; }
        public CombatSkillDirectory Directory { get; private set; }
        /// <summary>Result of the last cast attempt that consumed a skill press (sandbox read-out and tests).</summary>
        public CastResult LastResult { get; private set; } = CastResult.NoSkill;

        public bool IsReady => Controller != null;

        public void Setup(CombatWorld world, CombatActor actor, PlayerMotor motor, AttackController attack, ISkillResource resource,
            float weaponPower)
        {
            _world = world;
            _actor = actor;
            _motor = motor;
            _attack = attack;
            CombatEntity entity = actor.Entity;

            Caster = new SkillTarget
            {
                Id = entity.Id, Entity = entity, Receiver = actor.Receiver, State = actor.States, Shield = entity.Shield, Timed = actor.Timed,
            };
            Directory = new CombatSkillDirectory(world);
            Directory.Add(Caster);

            Context = new SkillExecContext(Caster, new PhysicsAreaQuery(), new SkillDamageCalculator(world.Config.BuildOffensive()),
                world.Rng, world.Events)
            {
                Directory = Directory,
                Motion = new MotorMotion(motor, entity),
                Resource = resource,
                Tethers = new TetherSet(),
                WeaponPower = weaponPower,
            };
            Specs = new SkillSpecCache(entity.Stats);
            Runner = new SkillRunner(Context);
            Controller = new SkillController(entity.Id, new StateComponentSkillState(actor.States), resource,
                new AttackControllerCancelHost(attack), Runner, world.Events);
            Context.Controller = Controller;

            world.Services.Scheduler.Register(this, TickPriority);
            _registered = true;
        }

        private void OnDestroy()
        {
            if (_registered && _world != null && _world.Services != null) _world.Services.Scheduler.Unregister(this);
            _registered = false;
        }

        public void Tick(in SimulationFrame frame)
        {
            if (Controller == null) return;
            CombatEntity entity = _actor.Entity;
            if (entity.Clock.IsFrozen) return; // hit-stop: the AttackController consumes the frozen tick

            int f = frame.Index;
            PlayerInputReader input = _world.Services.InputReader;
            Context.Frame = f;
            Context.ButtonHeld = input.IsHeld(InputActionId.Skill3);

            if (!entity.IsAlive)
            {
                Controller.Interrupt(f);
                ReleaseMoveLock();
                return;
            }

            Specs.Tick(f);
            Controller.Tick(f);
            Runner.Tick(f);

            if (Controller.IsCasting)
            {
                // Skill -> Basic/Heavy never cancels: drop the presses so the AttackController does not start a swing under the cast.
                input.Buffer.TryConsume(f, InputActionId.Basic.Bit() | InputActionId.Heavy.Bit(), out _);
            }
            else if (Controller.ProcessInput(input.Buffer, Specs, f, out CastResult result))
            {
                LastResult = result;
                if (result == CastResult.Started && _motor.Aim != null) _motor.SetFacing(_motor.Aim.AimDirection);
            }

            ApplyMoveLock();
        }

        private void ApplyMoveLock()
        {
            if (!Controller.IsCasting)
            {
                ReleaseMoveLock();
                return;
            }
            int percent = Controller.MoveDuringCastPercent;
            if (percent >= 100)
            {
                ReleaseMoveLock();
                return;
            }
            _motor.Lock = percent <= 0 ? MovementLock.Locked : MovementLock.Reduced;
            _moveLocked = true;
        }

        private void ReleaseMoveLock()
        {
            if (!_moveLocked) return;
            _moveLocked = false;
            if (!_attack.IsAttacking) _motor.Lock = MovementLock.Free;
        }
    }
}

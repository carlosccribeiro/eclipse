using Eclipse.Data.Combat;
using Eclipse.Data.Heroes;
using Eclipse.Data.Skills;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Attacks;
using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Player;
using Eclipse.Gameplay.Simulation;
using Eclipse.Gameplay.Skills;
using UnityEngine;
using UnityEngine.InputSystem;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Gameplay.Heroes.Kael
{
    /// <summary>
    /// Composition root of Kael on the player prefab (M08 T7): Fury (<see cref="ResourceComponent"/> + <see cref="KaelFury"/>), innate
    /// passives, the five skills through <see cref="SkillCasterRig"/>, Ashen Guard on the hurtbox receiver and the weapon swap.
    /// Rules stay in the pure classes; this shell only connects them to combat events (Fury gains, passive refresh) once per tick,
    /// after the attacks (-95) and the post-tick of the actor (-80).
    /// Sandbox debug keys (not part of the game input map): F5..F9 equip Greatsword, Dual Axes, Warhammer, Longsword, Heavy Blade.
    /// </summary>
    [DefaultExecutionOrder(-550)]
    public sealed class KaelHeroRig : MonoBehaviour
    {
        public const int PostTickPriority = -70;

        private sealed class Post : IFixedTickable
        {
            private readonly KaelHeroRig _rig;
            public Post(KaelHeroRig rig) { _rig = rig; }
            public void Tick(in SimulationFrame frame) => _rig.PostTick(frame.Index);
        }

        [SerializeField] private HeroDefinition hero;
        [Tooltip("Debug weapon order: F5..F9.")]
        [SerializeField] private WeaponDefinition[] weapons;
        [SerializeField] private CombatActor actor;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private DodgeController dodge;
        [SerializeField] private AttackController attack;
        [SerializeField] private ResourceComponent resource;
        [SerializeField] private SkillCasterRig skillRig;

        private CombatWorld _world;
        private KaelFury _fury;
        private KaelPassives _passives;
        private Post _post;
        private WeaponKitBonus _kit;
        private int _weaponIndex = -1;
        private bool _ready;

        public KaelFury Fury => _fury;
        public KaelPassives Passives => _passives;
        public SkillCasterRig Skills => skillRig;
        public AshenGuardReceiver Guard { get; private set; }
        public WeaponDefinition CurrentWeapon => _weaponIndex >= 0 ? weapons[_weaponIndex] : null;
        public int WeaponCount => weapons == null ? 0 : weapons.Length;
        public int WeaponIndex => _weaponIndex;
        public WeaponKitBonus Kit => _kit;

        private void Start()
        {
            _world = CombatWorld.Current;
            if (_world == null || actor == null || actor.Entity == null || hero == null || hero.Resource == null
                || weapons == null || weapons.Length == 0 || skillRig == null || resource == null)
            {
                Debug.LogError("KaelHeroRig: world/actor/hero/weapons/skill rig missing.", this);
                enabled = false;
                return;
            }

            CombatEntity entity = actor.Entity;
            resource.Initialize(hero.Resource, entity.Id, _world.Events);
            _fury = new KaelFury(resource.Pool, FuryGainRules.FromDefinition(hero.Resource), entity.Stats);
            _passives = new KaelPassives(entity.Stats, KaelPassiveRules.FromDefinition(hero.Resource));

            var skillResource = new ResourcePoolSkillResource(resource.Pool, publishesSpentEvent: true, () => _fury.GenerationMultiplier);
            skillRig.Setup(_world, actor, motor, attack, skillResource, weapons[0].WeaponPowerILvl1);

            var hurtbox = GetComponentInChildren<Hurtbox>();
            if (hurtbox != null)
            {
                Guard = new AshenGuardReceiver(actor.Receiver, skillRig.Context);
                hurtbox.Bind(entity, Guard);
            }

            for (int i = 0; i < hero.Skills.Count; i++)
                skillRig.Runner.Register(hero.Skills[i].skill);

            _post = new Post(this);
            _world.Services.Scheduler.Register(_post, PostTickPriority);
            _ready = true;
            Equip(0, force: true);
        }

        private void OnDestroy()
        {
            if (_ready && _world != null && _world.Services != null) _world.Services.Scheduler.Unregister(_post);
            _ready = false;
        }

        private void Update()
        {
            if (!_ready) return;
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            if (kb.f5Key.wasPressedThisFrame) Equip(0);
            if (kb.f6Key.wasPressedThisFrame) Equip(1);
            if (kb.f7Key.wasPressedThisFrame) Equip(2);
            if (kb.f8Key.wasPressedThisFrame) Equip(3);
            if (kb.f9Key.wasPressedThisFrame) Equip(4);
        }

        /// <summary>
        /// Equips weapon <paramref name="index"/>: re-initialises the attack controller with the weapon's moveset, refreshes Weapon Power and
        /// recompiles the five skill specs with the weapon kit bonus. Refused while an attack or a skill is running.
        /// </summary>
        public bool Equip(int index, bool force = false)
        {
            if (!_ready || index < 0 || index >= weapons.Length || weapons[index] == null) return false;
            if (!force && (index == _weaponIndex || attack.IsAttacking || skillRig.Controller.IsCasting)) return false;

            WeaponDefinition weapon = weapons[index];
            var evaluator = new HitboxEvaluator(new WeaponHitResolver(_world, weapon, attack));
            attack.Initialize(_world.Services.Scheduler, _world.Services.InputReader, motor, actor.Entity, evaluator, weapon, actor.States, dodge);
            _weaponIndex = index;
            _kit = WeaponKitBonus.ForWeapon(weapon.StableIdString);
            skillRig.Context.WeaponPower = weapon.WeaponPowerILvl1;

            for (int i = 0; i < hero.Skills.Count; i++)
            {
                SkillDefinition def = hero.Skills[i].skill;
                SkillSpecBuilder builder = KaelSkillFrames.Builder(def);
                ApplyKit(builder, def.StableIdString);
                skillRig.Specs.Register(builder);
            }
            return true;
        }

        // Kit bonuses that map onto a skill param (08 4.6): Greatsword area, Warhammer leap radius, Dual Axes extra chains.
        // Parry window, counter damage, Stagger Damage and execute bonuses are not wired yet (see the M08 Log).
        private void ApplyKit(SkillSpecBuilder b, string skillId)
        {
            float v;
            if (skillId == KaelSkillIds.CleavingStrike && _kit.CleavingStrikeAreaIncrease > 0f && b.TryGetParam("radius_m", out v))
                b.SetParam("radius_m", WeaponKitBonus.ScaleRadius(v, _kit.CleavingStrikeAreaIncrease));
            else if (skillId == KaelSkillIds.InfernalLeap && _kit.LeapRadiusBonusMeters > 0f && b.TryGetParam("radius_m", out v))
                b.SetParam("radius_m", v + _kit.LeapRadiusBonusMeters);
            else if (skillId == KaelSkillIds.BurningChains && _kit.BurningChainsExtraChains > 0 && b.TryGetParam("max_targets", out v))
                b.SetParam("max_targets", v + _kit.BurningChainsExtraChains);
        }

        private void PostTick(int f)
        {
            CombatEntity entity = actor.Entity;
            resource.SetFrame(f);
            _fury.Tick(SimulationFrame.FixedDeltaSeconds);

            // Fury from this tick's landed hits of the owner (Generation 0 only: direct player actions).
            var hits = _world.Events.Read<OnHitDealt>();
            int landed = 0;
            for (int i = 0; i < hits.Count; i++)
            {
                ref readonly OnHitDealt h = ref hits[i];
                if (h.Frame == f && h.Source == entity.Id && h.Generation == 0) landed++;
            }
            if (landed > 0)
            {
                _fury.OnDamageDealt();
                if (!skillRig.Controller.IsCasting && attack.IsAttacking)
                {
                    AttackRuntimeState state = attack.Sequencer.State;
                    if (state.Kind == AttackKind.Heavy) _fury.OnHeavyHit(state.ChargeFraction >= 0.999f);
                    else
                    {
                        _fury.OnBasicHit(landed);
                        if (_kit.FuryPerBasic > 0f) resource.Pool.Add(_kit.FuryPerBasic * _fury.GenerationMultiplier);
                    }
                }
            }

            _fury.ConsumeEvents(_world.Events, entity.Id, entity.Health.Max);
            _passives.Refresh(resource.Pool.Current, entity.Health.Current, entity.Health.Max);
        }
    }
}

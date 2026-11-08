using System.Collections.Generic;
using Eclipse.AI.Director;
using Eclipse.AI.Locomotion;
using Eclipse.AI.Lod;
using Eclipse.AI.Telegraphs;
using Eclipse.Data.Difficulties;
using Eclipse.Data.Enemies;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Pooling;
using UnityEngine;

namespace Eclipse.AI.Enemies
{
    /// <summary>
    /// Scene-level collaborators every <see cref="EnemyController"/> needs, built once by the spawner (T8) or a test. Plain C#:
    /// the M05 services (projectiles, areas) are plain classes the composition root owns and ticks, so they are handed in here.
    /// Also keeps the list of live enemies (ally queries) and the attack plans by hit group (arc landings).
    /// </summary>
    public sealed class EnemyServices
    {
        private readonly Dictionary<(EnemyDefinition, int), EnemyBlueprint> _blueprints = new Dictionary<(EnemyDefinition, int), EnemyBlueprint>();
        private readonly Dictionary<int, AttackPlan> _plansByHitGroup = new Dictionary<int, AttackPlan>();
        private Matrix4x4 _viewProjection;
        private int _cameraFrame = -1;

        public EnemyServices(CombatWorld world, LocomotionSystem locomotion, AttackTokenDirector tokens, PoolService pool,
            TelegraphViewPool telegraphs, TelegraphPalette palette, ProjectileManager projectiles, AreaDeliverer areas,
            EnemyScalingDefinition scaling, DifficultyDefinition difficulty, DecisionLodConfig lod)
        {
            World = world;
            Locomotion = locomotion;
            Tokens = tokens;
            Pool = pool;
            Telegraphs = telegraphs;
            Palette = palette;
            Projectiles = projectiles;
            Areas = areas;
            Scaling = scaling;
            Difficulty = difficulty;
            Lod = lod;
        }

        public CombatWorld World { get; }
        public LocomotionSystem Locomotion { get; }
        public AttackTokenDirector Tokens { get; }
        public PoolService Pool { get; }
        /// <summary>May be null (no telegraph visuals; gameplay timing is unaffected).</summary>
        public TelegraphViewPool Telegraphs { get; }
        public TelegraphPalette Palette { get; }
        public ProjectileManager Projectiles { get; }
        public AreaDeliverer Areas { get; }
        public EnemyScalingDefinition Scaling { get; }
        public DifficultyDefinition Difficulty { get; }
        public DecisionLodConfig Lod { get; }

        /// <summary>Optional: Burning pools and other zone statuses. Null skips the status (the area still resolves).</summary>
        public EnemyStatusZones StatusZones { get; set; }

        /// <summary>Camera used for the off-screen rule; null counts everything as on-screen.</summary>
        public Camera Camera { get; set; }

        /// <summary>Viewport margin (fraction) that still counts as on-screen. TUNED: a small band so edge cases do not flicker.</summary>
        public float OnScreenMargin { get; set; } = 0.05f;

        /// <summary>
        /// Apex of a lob as a fraction of its planar distance (artillery arc, 09 4.2). TUNED: no doc gives the arc height.
        /// </summary>
        public float LobApexFraction { get; set; } = 0.25f;

        /// <summary>Live enemies, for ally counts, escort and the "no shot through a Tank" rule.</summary>
        public List<EnemyController> Active { get; } = new List<EnemyController>(256);

        /// <summary>Blueprint for an enemy at a Monster Level (cached; plans are immutable and shared by all instances).</summary>
        public bool TryGetBlueprint(EnemyDefinition enemy, int level, out EnemyBlueprint blueprint)
        {
            if (_blueprints.TryGetValue((enemy, level), out blueprint)) return true;
            if (!EnemyStatsBuilder.TryBuild(enemy, Scaling, level, Difficulty, out EnemyStats stats)) return false;
            float reduction = EnemyStatsBuilder.WindupReduction(Difficulty);
            blueprint = EnemyBlueprint.Build(enemy, in stats, reduction);
            _blueprints[(enemy, level)] = blueprint;
            RegisterPlans(blueprint);
            return true;
        }

        public void RegisterPlans(EnemyBlueprint blueprint)
        {
            for (int i = 0; i < blueprint.Heads.Length; i++) RegisterChain(blueprint.Heads[i]);
            for (int i = 0; i < blueprint.DeathPlans.Length; i++) RegisterChain(blueprint.DeathPlans[i]);
        }

        private void RegisterChain(AttackPlan plan)
        {
            for (AttackPlan p = plan; p != null; p = p.Next) _plansByHitGroup[p.HitGroup] = p;
        }

        public bool TryGetPlan(int hitGroup, out AttackPlan plan) => _plansByHitGroup.TryGetValue(hitGroup, out plan);

        /// <summary>06 section 3.10 off-screen rule input. The matrix is refreshed once per simulation frame.</summary>
        public bool IsOnScreen(Vector3 worldPoint, int frame)
        {
            if (Camera == null) return true;
            if (_cameraFrame != frame)
            {
                _viewProjection = Camera.projectionMatrix * Camera.worldToCameraMatrix;
                _cameraFrame = frame;
            }
            return ShotLine.IsOnScreen(in _viewProjection, worldPoint, OnScreenMargin);
        }
    }
}

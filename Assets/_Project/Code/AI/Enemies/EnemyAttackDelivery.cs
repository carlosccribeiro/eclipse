using Eclipse.AI.Telegraphs;
using Eclipse.Core.Ids;
using Eclipse.Core.Tags;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Simulation;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.AI.Enemies
{
    /// <summary>
    /// Unity side of an enemy attack: shows the telegraph from the pool and, on impact, fires the hit through the M05 services
    /// (instant <see cref="AreaSpec"/> for melee/area shapes, <see cref="ProjectileSpec"/> for projectiles and lobs, a status pool
    /// for zones). Damage is already absolute (curve x multipliers x <c>damage_rel</c>); the player's incoming pipeline mitigates.
    /// One instance per enemy; no allocation per call.
    /// </summary>
    public sealed class EnemyAttackDelivery : IAttackDelivery
    {
        private readonly EnemyServices _services;
        private readonly EntityId _source;
        private readonly float _muzzleHeight;
        private TelegraphView _view;

        public EnemyAttackDelivery(EnemyServices services, EntityId source, float muzzleHeight)
        {
            _services = services;
            _source = source;
            _muzzleHeight = muzzleHeight;
        }

        public void TelegraphStart(AttackPlan plan, in AttackAim aim)
        {
            ReleaseView();
            if (_services.Telegraphs == null || _services.Palette == null) return;
            _view = _services.Telegraphs.Rent();
            if (_view == null) return; // pool exhausted: the timing is unaffected, only the visual is skipped
            TelegraphRequest request = AttackGeometry.ToTelegraph(plan, in aim);
            _view.Show(in request, _services.Palette);
        }

        public void TelegraphProgress(float progress)
        {
            if (_view != null) _view.SetProgress(progress);
        }

        public void TelegraphEnd(bool completed) => ReleaseView();

        public void Launch(AttackPlan plan, in AttackAim aim)
        {
            if (_services.Projectiles == null) return;
            Vector3 from = aim.Origin + Vector3.up * _muzzleHeight;
            Vector3 flat = aim.Target - aim.Origin;
            flat.y = 0f;
            float distance = flat.magnitude;
            if (distance < 1e-3f || plan.ProjectileSpeed <= 0f) return;
            var spec = new ProjectileSpec(_source, Team.Enemy, from, flat / distance, plan.ProjectileSpeed, ProjectileManager.MinCastRadius,
                distance, 0, plan.DamageType, plan.Damage, plan.StaggerValue, 1f, 0, plan.HitGroup, plan.Tags, plan.Flags);
            _services.Projectiles.Spawn(in spec, ProjectileBehavior.Artillery(aim.Target, distance * _services.LobApexFraction));
        }

        public void Impact(AttackPlan plan, in AttackAim aim)
        {
            if (plan.IsLobbed) return; // the landing handler detonates when the arc touches down (EnemyProjectileLandings)
            if (plan.IsStraightProjectile)
            {
                FireStraight(plan, in aim);
                return;
            }
            if (!AttackGeometry.TryShape(plan, out HitboxShape shape)) return;
            Vector3 origin = AttackGeometry.ShapeOrigin(plan, in aim);
            var area = new AreaSpec(_source, Team.Enemy, shape, origin, aim.Forward, 0, 0, plan.DamageType, plan.Damage,
                plan.StaggerValue, 1f, 0, plan.HitGroup, plan.Tags, plan.Flags);
            _services.Areas?.Detonate(in area, _services.World.Services.Scheduler.CurrentFrame);
            SpawnZone(plan, origin, _source, _services);
        }

        private void FireStraight(AttackPlan plan, in AttackAim aim)
        {
            if (_services.Projectiles == null || plan.ProjectileSpeed <= 0f) return;
            Vector3 from = aim.Origin + Vector3.up * _muzzleHeight;
            float radius = plan.Width > 0f ? plan.Width * 0.5f : ProjectileManager.MinCastRadius;
            var spec = new ProjectileSpec(_source, Team.Enemy, from, aim.Forward, plan.ProjectileSpeed, radius, plan.MaxRange, 0,
                plan.DamageType, plan.Damage, plan.StaggerValue, 1f, 0, plan.HitGroup, plan.Tags, plan.Flags);
            _services.Projectiles.Spawn(in spec);
        }

        /// <summary>Leaves the status pool of an attack (<c>zone_status</c>, <c>zone_duration_s</c>) at a position. No-op when it has none.</summary>
        public static void SpawnZone(AttackPlan plan, Vector3 origin, EntityId source, EnemyServices services)
        {
            if (services.StatusZones == null || string.IsNullOrEmpty(plan.ZoneStatus) || plan.ZoneDurationSeconds <= 0f) return;
            if (!StableId.TryParse(StatusSlug(plan.ZoneStatus), out StableId status)) return;
            int frames = Mathf.CeilToInt(plan.ZoneDurationSeconds * SimulationFrame.TicksPerSecond);
            services.StatusZones.Spawn(origin, plan.Radius, frames, source, status);
        }

        /// <summary>CSV status name to slug (<c>Burning</c> to <c>status.burning</c>, matching <c>StatusStubDefinition</c>).</summary>
        public static string StatusSlug(string zoneStatus) => "status." + zoneStatus.ToLowerInvariant();

        private void ReleaseView()
        {
            if (_view == null) return;
            _services.Telegraphs.Return(_view);
            _view = null;
        }
    }

    /// <summary>
    /// Detonates the landing of enemy lobs (<see cref="IProjectileListener"/> of the <see cref="ProjectileManager"/>): looks the
    /// attack up by the hit group carried in the spec, applies the circle at the landing point and leaves the status pool.
    /// Set it as <c>ProjectileManager.Listener</c> when composing the scene.
    /// </summary>
    public sealed class EnemyProjectileLandings : IProjectileListener
    {
        private readonly EnemyServices _services;

        public EnemyProjectileLandings(EnemyServices services)
        {
            _services = services;
        }

        public void OnArcLanded(in ProjectileSpec spec, Vector3 position, int frame)
        {
            if (spec.Team != Team.Enemy || !_services.TryGetPlan(spec.HitGroupId, out AttackPlan plan)) return;
            if (!AttackGeometry.TryShape(plan, out HitboxShape shape)) return;
            var area = new AreaSpec(spec.Source, Team.Enemy, shape, position, spec.Direction, 0, 0, spec.Type, spec.Damage,
                spec.StaggerValue, spec.ProcCoefficient, spec.Generation, spec.HitGroupId, spec.Tags, spec.Flags);
            _services.Areas?.Detonate(in area, frame);
            EnemyAttackDelivery.SpawnZone(plan, position, spec.Source, _services);
        }
    }
}

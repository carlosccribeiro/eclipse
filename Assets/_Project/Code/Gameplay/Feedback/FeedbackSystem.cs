using Eclipse.Gameplay.Cameras;
using Eclipse.Gameplay.Combat;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Gameplay.Feedback
{
    /// <summary>
    /// Reads the <see cref="CombatEventStream"/> and drives hit-stop, camera trauma, flash/squash and VFX_/SFX_ hook
    /// events (06 3.11). Never touches UI/Audio: hooks are published as events. Call <see cref="Process"/> once per
    /// tick after combat has published and before the stream is cleared. No allocation per call.
    /// </summary>
    public sealed class FeedbackSystem
    {
        private readonly CombatEventStream _events;
        private readonly IFeedbackWorld _world;
        private readonly IHitFeedbackSource _source;
        private readonly CameraShakeService _shake;
        private readonly HitStopService _hitStop;
        private readonly FeedbackRules _rules;

        public FeedbackSystem(CombatEventStream events, IFeedbackWorld world, IHitFeedbackSource source,
            CameraShakeService shake, FeedbackRules rules)
        {
            _events = events;
            _world = world;
            _source = source;
            _shake = shake;
            _rules = rules;
            _hitStop = new HitStopService(rules);
            events.Prewarm<OnVfxHook>();
            events.Prewarm<OnSfxHook>();
        }

        public HitStopService HitStop => _hitStop;

        /// <summary>Phase change or any other external request for a world-wide freeze.</summary>
        public void RequestGlobalHitStop(int frame, int frames)
        {
            _world.FreezeAll(frames);
            _events.Publish(new OnVfxHook(frame, VfxHookId.VFX_GlobalHitStop, EntityId.None, Vector3.zero));
            _events.Publish(new OnSfxHook(frame, SfxHookId.SFX_GlobalHitStop, EntityId.None, Vector3.zero));
        }

        public void Process()
        {
            var hits = _events.Read<OnHitDealt>();
            for (int i = 0; i < hits.Count; i++) OnHit(hits[i]);

            var kills = _events.Read<OnKill>();
            for (int i = 0; i < kills.Count; i++) OnKilled(kills[i]);

            var walls = _events.Read<OnWallImpact>();
            for (int i = 0; i < walls.Count; i++)
            {
                var w = walls[i];
                _shake?.AddTrauma(TraumaTable.WallImpact(_rules));
                _events.Publish(new OnVfxHook(w.Frame, VfxHookId.VFX_WallImpact, w.Target, w.Position));
                _events.Publish(new OnSfxHook(w.Frame, SfxHookId.SFX_WallImpact, w.Target, w.Position));
            }

            var globals = _events.Read<OnGlobalHitStop>();
            for (int i = 0; i < globals.Count; i++) RequestGlobalHitStop(globals[i].Frame, globals[i].Frames);

            var taken = _events.Read<OnHitTaken>();
            for (int i = 0; i < taken.Count; i++)
            {
                var t = taken[i];
                if (t.Result == HitResult.Hit && t.Value > 0f && _world.IsBoss(t.Source))
                    _shake?.AddTrauma(TraumaTable.BossHitReceived(_rules));
            }
        }

        private void OnHit(in OnHitDealt hit)
        {
            if (!_source.TryGet(in hit, out var info)) return;

            var plan = _hitStop.Plan(hit.Source.Value, hit.Frame, in info, hit.IsCrit);
            if (plan.AttackerFrames > 0 && _world.TryGetClock(hit.Source, out var ac)) HitStopService.Freeze(ac, plan.AttackerFrames);
            if (plan.TargetFrames > 0 && _world.TryGetClock(hit.Target, out var tc)) HitStopService.Freeze(tc, plan.TargetFrames);

            float trauma = TraumaTable.Hit(in info, hit.IsCrit, in _rules);
            if (trauma > 0f) _shake?.AddTrauma(trauma);

            if (_world.TryGetVisual(hit.Target, out var visual))
            {
                if (info.Values.FlashFrames > 0) visual.Flash(info.Values.FlashFrames);
                if (info.Values.SquashSeconds > 0f) visual.Squash(info.Values.SquashSeconds);
            }

            _events.Publish(new OnVfxHook(hit.Frame, hit.IsCrit ? VfxHookId.VFX_CritImpact : VfxHookId.VFX_HitImpact, hit.Target, hit.Position));
            _events.Publish(new OnSfxHook(hit.Frame, hit.IsCrit ? SfxHookId.SFX_Crit : SfxHookId.SFX_Hit, hit.Target, hit.Position));

            if (info.IsUltimate) RequestGlobalHitStop(hit.Frame, _rules.GlobalHitStopFrames);
        }

        private void OnKilled(in OnKill kill)
        {
            if (kill.TargetIsElite && _world.TryGetClock(kill.Source, out var clock))
                HitStopService.Freeze(clock, _hitStop.EliteKillFrames(kill.Source.Value, kill.Frame));
            _events.Publish(new OnVfxHook(kill.Frame, VfxHookId.VFX_Kill, kill.Target, kill.Position));
            _events.Publish(new OnSfxHook(kill.Frame, SfxHookId.SFX_Kill, kill.Target, kill.Position));
        }
    }
}

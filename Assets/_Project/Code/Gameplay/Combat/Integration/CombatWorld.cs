using System.Collections.Generic;
using Eclipse.Core.Random;
using Eclipse.Gameplay.Feedback;
using Eclipse.Gameplay.Simulation;
using UnityEngine;

namespace Eclipse.Gameplay.Combat.Integration
{
    /// <summary>
    /// Scene composition root of combat (M05 T10): owns the <see cref="CombatEventStream"/>, RNG streams, state rules,
    /// entity registry and the <see cref="FeedbackSystem"/>. Implements <see cref="IFeedbackWorld"/> and
    /// <see cref="IHitFeedbackSource"/> for the feedback layer. Feedback and Flask refunds are processed once per
    /// rendered frame in LateUpdate (so a FixedUpdate with two ticks never processes an event twice); the stream is
    /// cleared afterwards by <see cref="CombatStreamFlusher"/> (UI/Audio read in between, 11 3.7).
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class CombatWorld : MonoBehaviour, IFeedbackWorld, IHitFeedbackSource
    {
        private const int HitLogSize = 64;

        private struct HitLogEntry
        {
            public int Frame, Source, Target;
            public HitFeedbackInfo Info;
            public bool Used;
        }

        [SerializeField] private CombatRuntimeConfig config;

        private readonly List<CombatActor> _actors = new List<CombatActor>(32);
        private readonly Dictionary<int, CombatActor> _byId = new Dictionary<int, CombatActor>(32);
        private readonly HitLogEntry[] _log = new HitLogEntry[HitLogSize];
        private int _logHead;
        private HitFeedbackInfo _staged;
        private bool _hasStaged;
        private int _nextId;

        public static CombatWorld Current { get; private set; }

        public CombatRuntimeConfig Config => config;
        public CombatEventStream Events { get; private set; }
        public RngStreams Rng { get; private set; }
        public CombatStateRules Rules { get; private set; }
        public FeedbackSystem Feedback { get; private set; }
        public GameplayServices Services { get; private set; }
        public IReadOnlyList<CombatActor> Actors => _actors;
        public CombatActor Player { get; private set; }
        /// <summary>Hit-stop/trauma/flash policy; replace before the first hit to change the rules.</summary>
        public FeedbackRules FeedbackRules { get; set; } = FeedbackRules.Doc;

        private void Awake()
        {
            Current = this;
            if (config == null || config.combatStates == null)
            {
                Debug.LogError("CombatWorld: CombatRuntimeConfig (with CombatStates) missing.", this);
                enabled = false;
                return;
            }
            Services = FindFirstObjectByType<GameplayServices>();
            if (Services == null)
            {
                Debug.LogError("CombatWorld: GameplayServices not found (add GameplayBootstrap.prefab).", this);
                enabled = false;
                return;
            }
            Events = new CombatEventStream();
            Rng = new RngStreams(config.rngSeed);
            Rules = new CombatStateRules(config.combatStates.Values);
            Feedback = new FeedbackSystem(Events, this, this, Services.CameraRig != null ? Services.CameraRig.Shake : null, FeedbackRules);
            Events.Prewarm<OnHitDealt>();
            Events.Prewarm<OnKill>();
            if (GetComponent<CombatStreamFlusher>() == null) gameObject.AddComponent<CombatStreamFlusher>().Bind(this);
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public EntityId AllocateId() => new EntityId(++_nextId);

        public void Register(CombatActor actor)
        {
            _actors.Add(actor);
            _byId[actor.Entity.Id.Value] = actor;
            if (actor.IsPlayer) Player = actor;
        }

        public void Unregister(CombatActor actor)
        {
            _actors.Remove(actor);
            if (actor.Entity != null) _byId.Remove(actor.Entity.Id.Value);
            if (Player == actor) Player = null;
        }

        public bool TryGetActor(EntityId id, out CombatActor actor) => _byId.TryGetValue(id.Value, out actor);

        // ------------------------------------------------------------------ hit feedback log

        /// <summary>Called by the attacker's resolver right before the receiver runs (same call stack).</summary>
        public void StageHitInfo(in HitFeedbackInfo info)
        {
            _staged = info;
            _hasStaged = true;
        }

        /// <summary>Called by the receiver after the hit: files the staged info under (frame, source, target).</summary>
        public void CommitStagedHit(int frame, EntityId source, EntityId target)
        {
            if (_hasStaged)
            {
                _log[_logHead] = new HitLogEntry { Frame = frame, Source = source.Value, Target = target.Value, Info = _staged, Used = true };
                _logHead = (_logHead + 1) % HitLogSize;
            }
            _hasStaged = false;
        }

        public void DiscardStagedHit() => _hasStaged = false;

        public bool TryGet(in OnHitDealt hit, out HitFeedbackInfo info)
        {
            for (int i = 0; i < HitLogSize; i++)
            {
                ref HitLogEntry e = ref _log[i];
                if (!e.Used || e.Frame != hit.Frame || e.Source != hit.Source.Value || e.Target != hit.Target.Value) continue;
                info = e.Info;
                return true;
            }
            info = default;
            return false;
        }

        // ------------------------------------------------------------------ IFeedbackWorld

        public bool TryGetClock(EntityId id, out EntityClock clock)
        {
            if (_byId.TryGetValue(id.Value, out CombatActor a) && a.Entity != null)
            {
                clock = a.Entity.Clock;
                return true;
            }
            clock = null;
            return false;
        }

        public void FreezeAll(int frames)
        {
            for (int i = 0; i < _actors.Count; i++)
                if (_actors[i].Entity != null) _actors[i].Entity.Clock.Freeze(frames);
        }

        public bool IsBoss(EntityId id) => _byId.TryGetValue(id.Value, out CombatActor a) && a.IsBoss;

        public bool TryGetVisual(EntityId id, out IImpactVisual visual)
        {
            if (_byId.TryGetValue(id.Value, out CombatActor a)) { visual = a; return true; }
            visual = null;
            return false;
        }

        // ------------------------------------------------------------------ per-frame processing

        private void LateUpdate() => ProcessEvents();

        /// <summary>Feedback + Flask refunds for the events of this frame. Tests may call it manually after stepping the scheduler.</summary>
        public void ProcessEvents()
        {
            if (Feedback == null) return;
            Feedback.Process();
            var kills = Events.Read<OnKill>();
            for (int i = 0; i < kills.Count; i++)
            {
                OnKill k = kills[i];
                for (int a = 0; a < _actors.Count; a++)
                    if (_actors[a].Flask != null) _actors[a].Flask.HandleKill(in k, 0);
            }
        }
    }
}

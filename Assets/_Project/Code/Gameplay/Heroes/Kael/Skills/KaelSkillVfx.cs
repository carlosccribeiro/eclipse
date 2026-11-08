using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Skills;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Gameplay.Heroes.Kael
{
    /// <summary>
    /// Placeholder skill VFX (M08 T7): a pooled flat disc per cast, tinted by affinity (06 3.11: Physical white, Ember orange,
    /// Ruin sickly green), placed where the skill acts. Reads OnSkillCast from the combat stream in LateUpdate (before the stream is
    /// cleared); discs are pre-instantiated and recycled, so nothing allocates while playing. Purely presentational.
    /// Sizes come from the skill params already in the spec (radius_m / range_m / counter_radius_m), never from literals here.
    /// </summary>
    public sealed class KaelSkillVfx : MonoBehaviour
    {
        // Visual-only constants (not balance): how long a disc stays and how many fit at once.
        private const float DiscSeconds = 0.4f;
        private const int PoolSize = 6;

        [SerializeField] private KaelHeroRig rig;
        [SerializeField] private GameObject emberPrefab;
        [SerializeField] private GameObject physicalPrefab;
        [SerializeField] private GameObject ruinPrefab;

        private GameObject[] _pool;
        private float[] _until;
        private int _next;
        private bool _ready;

        public int PlayedCount { get; private set; }

        private void Start()
        {
            if (rig == null || emberPrefab == null || physicalPrefab == null || ruinPrefab == null)
            {
                Debug.LogError("KaelSkillVfx: rig/prefabs missing.", this);
                enabled = false;
                return;
            }
            GameObject[] sources = { emberPrefab, physicalPrefab, ruinPrefab };
            _pool = new GameObject[PoolSize * sources.Length];
            _until = new float[_pool.Length];
            for (int i = 0; i < _pool.Length; i++)
            {
                _pool[i] = Instantiate(sources[i / PoolSize]); // world space: discs must not follow the caster
                _pool[i].SetActive(false);
            }
            _ready = true;
        }

        private void Update()
        {
            if (!_ready) return;
            float now = Time.time;
            for (int i = 0; i < _pool.Length; i++)
                if (_pool[i].activeSelf && now >= _until[i]) _pool[i].SetActive(false);
        }

        private void LateUpdate()
        {
            CombatWorld world = CombatWorld.Current;
            if (!_ready || world == null || world.Events == null || rig.Skills == null || !rig.Skills.IsReady) return;
            EntityId owner = rig.Skills.Caster.Id;
            var casts = world.Events.Read<OnSkillCast>();
            for (int i = 0; i < casts.Count; i++)
            {
                ref readonly OnSkillCast c = ref casts[i];
                if (c.Source == owner) Play(c.Skill.Value);
            }
        }

        private void Play(string skillId)
        {
            SkillExecContext ctx = rig.Skills.Context;
            SkillSpec spec = ctx.Cast.Spec;
            if (spec == null || spec.SkillId.Value != skillId) return;

            Vector3 origin = ctx.CasterPosition;
            Vector3 forward = ctx.Forward;
            int group;
            Vector3 center = origin;
            float radius;
            switch (skillId)
            {
                case KaelSkillIds.CleavingStrike:
                    group = 1;
                    radius = SkillParams.Get(spec, "radius_m");
                    center = origin + forward * (radius * 0.5f);
                    break;
                case KaelSkillIds.InfernalLeap:
                    group = 0;
                    radius = SkillParams.Get(spec, "radius_m");
                    center = LeapTrajectory.Landing(origin, ctx.AimPoint, forward, SkillParams.Get(spec, "range_m"));
                    break;
                case KaelSkillIds.BurningChains:
                    group = 0;
                    radius = SkillParams.Get(spec, "range_m") * 0.5f;
                    center = origin + forward * radius;
                    break;
                case KaelSkillIds.AshenGuard:
                    group = 1;
                    radius = SkillParams.Get(spec, "counter_radius_m");
                    break;
                case KaelSkillIds.Worldbreaker:
                    group = 2;
                    radius = SkillParams.Get(spec, "radius_m");
                    break;
                default:
                    return;
            }
            Spawn(group, center, radius);
        }

        private void Spawn(int group, Vector3 center, float radius)
        {
            int slot = group * PoolSize + _next % PoolSize;
            _next++;
            GameObject disc = _pool[slot];
            disc.transform.SetPositionAndRotation(new Vector3(center.x, 0.05f, center.z), Quaternion.identity);
            disc.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
            disc.SetActive(true);
            _until[slot] = Time.time + DiscSeconds;
            PlayedCount++;
        }
    }
}

using Eclipse.Core.Random;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>
    /// Reads named numbers of a <see cref="SkillSpec"/> for Deliveries/Effects (Skills.csv column "params"). A missing key is a data
    /// error and throws. Keys ending in "_pct" are authored as 60 = 60% and come back as the fraction 0.6.
    /// </summary>
    public static class SkillParams
    {
        public static float Get(SkillSpec spec, string key)
        {
            float v = spec.GetParam(key);
            return key.EndsWith("_pct", System.StringComparison.Ordinal) ? v / 100f : v;
        }

        /// <summary>Value of <paramref name="key"/> when it is non-empty, otherwise <paramref name="literal"/>.</summary>
        public static float Resolve(SkillSpec spec, string key, float literal)
        {
            return string.IsNullOrEmpty(key) ? literal : Get(spec, key);
        }

        /// <summary>Stable int of a text key (FNV-1a), for refresh/source keys. Never <c>string.GetHashCode</c>.</summary>
        public static int KeyHash(string key)
        {
            return string.IsNullOrEmpty(key) ? 0 : (int)(RngStreams.Fnv1a64(key) & 0x7FFFFFFF);
        }
    }
}

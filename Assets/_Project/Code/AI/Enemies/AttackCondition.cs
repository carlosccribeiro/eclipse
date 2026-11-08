using System.Globalization;

namespace Eclipse.AI.Enemies
{
    /// <summary>
    /// Reads the free-text availability condition of an <c>AttackDefinition</c> (09 section 3.1). Only distance bounds are
    /// understood (<c>target_dist&lt;=2.0</c>, <c>target_dist&gt;=5.0</c>, also with <c>&lt;</c>/<c>&gt;</c>); markers such as
    /// <c>chain_only</c> or <c>on_death</c> mean "no distance bound". Runs at spawn time only.
    /// </summary>
    public static class AttackCondition
    {
        private const string Key = "target_dist";

        /// <summary>Applies the bounds found in <paramref name="condition"/> on top of the given defaults.</summary>
        public static void Parse(string condition, ref float min, ref float max)
        {
            if (string.IsNullOrWhiteSpace(condition)) return;
            string[] parts = condition.Split('&', ',', ';');
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i].Trim();
                if (!p.StartsWith(Key)) continue;
                string rest = p.Substring(Key.Length).Trim();
                bool lower = rest.StartsWith(">");
                bool upper = rest.StartsWith("<");
                if (!lower && !upper) continue;
                rest = rest.TrimStart('<', '>', '=').Trim();
                if (!float.TryParse(rest, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)) continue;
                if (lower) min = v; else max = v;
            }
        }
    }
}

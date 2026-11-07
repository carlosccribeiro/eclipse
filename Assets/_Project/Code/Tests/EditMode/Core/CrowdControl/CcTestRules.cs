namespace Eclipse.Core.CrowdControl.Tests
{
    /// <summary>Test fixture with the values of 06 §3.8.3 / R09 (runtime code holds no defaults).</summary>
    internal static class CcTestRules
    {
        // 06 §3.8.3: 8 s window, 100/50/25%; player Hard 1.5 s, Freeze 1.0 s, Root 2 s, 2 s Hard immunity.
        public static readonly CcRules Rules = new CcRules(8f, new[] { 1f, 0.5f, 0.25f }, 1.5f, 1.0f, 2.0f, 2.0f);

        public static CrowdControlTracker NewTracker() => new CrowdControlTracker(Rules);
    }
}

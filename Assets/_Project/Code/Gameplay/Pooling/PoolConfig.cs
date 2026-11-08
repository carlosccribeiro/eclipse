namespace Eclipse.Gameplay.Pooling
{
    /// <summary>Pool sizing. Values come from callers/data; none are balance numbers (11 §3.14).</summary>
    public readonly struct PoolConfig
    {
        /// <summary>Instances created up front.</summary>
        public readonly int Prewarm;
        /// <summary>Max idle instances kept; extra released ones are discarded. 0 = unbounded.</summary>
        public readonly int MaxIdle;

        public PoolConfig(int prewarm, int maxIdle = 0)
        {
            Prewarm = prewarm < 0 ? 0 : prewarm;
            MaxIdle = maxIdle < 0 ? 0 : maxIdle;
        }
    }
}

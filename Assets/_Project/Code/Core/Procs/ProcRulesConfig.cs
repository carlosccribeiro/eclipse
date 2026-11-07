using System;

namespace Eclipse.Core.Procs
{
    /// <summary>Kind of derived hit whose Proc Coefficient is fixed by 06 3.2 (R07).</summary>
    public enum DerivedProcKind
    {
        /// <summary>Echo repeat (06 3.2): coefficient multiplies the source coefficient.</summary>
        Echo = 0,
        /// <summary>Clone copy (06 3.2): coefficient multiplies the source coefficient.</summary>
        Clone = 1,
        /// <summary>Persistent area tick (06 3.2): fixed coefficient, also multiplied by the source coefficient.</summary>
        AreaTick = 2,
    }

    /// <summary>Balance values of the proc rules; supplied by data (06 3.2: Echo 0.5, Clone 0.4, area tick 0.25).</summary>
    public readonly struct ProcRulesConfig
    {
        public float EchoCoefficient { get; }
        public float CloneCoefficient { get; }
        public float AreaTickCoefficient { get; }

        public ProcRulesConfig(float echoCoefficient, float cloneCoefficient, float areaTickCoefficient)
        {
            if (echoCoefficient < 0f || echoCoefficient > 1f) throw new ArgumentOutOfRangeException(nameof(echoCoefficient));
            if (cloneCoefficient < 0f || cloneCoefficient > 1f) throw new ArgumentOutOfRangeException(nameof(cloneCoefficient));
            if (areaTickCoefficient < 0f || areaTickCoefficient > 1f) throw new ArgumentOutOfRangeException(nameof(areaTickCoefficient));
            EchoCoefficient = echoCoefficient;
            CloneCoefficient = cloneCoefficient;
            AreaTickCoefficient = areaTickCoefficient;
        }
    }
}

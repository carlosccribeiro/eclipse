using System;
using Eclipse.Core.Tags;

namespace Eclipse.Core.Procs
{
    /// <summary>Generation, replication and Proc Coefficient rules (06 3.2, 07 Generation, R07).</summary>
    public sealed class ProcRules
    {
        private readonly ProcRulesConfig _config;

        public ProcRules(ProcRulesConfig config)
        {
            _config = config;
        }

        /// <summary>06 3.2: nothing triggers at Generation >= 2.</summary>
        public bool CanTrigger(in ProcContext source) => source.Generation < ProcContext.MaxGeneration;

        /// <summary>Generation of a proc caused by the source (Gen + 1, capped at the maximum).</summary>
        public int NextGeneration(in ProcContext source) =>
            source.Generation >= ProcContext.MaxGeneration ? ProcContext.MaxGeneration : source.Generation + 1;

        /// <summary>
        /// 06 3.2 Replicas: a replica never spawns another replica, except with "Echo Recursion 1x" and only once,
        /// still subject to Generation below 2. An original (non-echo) hit may be replicated while Gen is below 2.
        /// </summary>
        public bool CanReplicate(in ProcContext source)
        {
            if (!CanTrigger(source)) return false;
            if (!source.IsEcho) return true;
            return source.AllowEchoRecursionOnce && !source.EchoRecursionUsed;
        }

        /// <summary>Proc Coefficient of a derived hit: fixed kind coefficient multiplying the source coefficient (06 3.2).</summary>
        public float DerivedProcCoefficient(DerivedProcKind kind, float sourceCoefficient)
        {
            float k;
            switch (kind)
            {
                case DerivedProcKind.Echo: k = _config.EchoCoefficient; break;
                case DerivedProcKind.Clone: k = _config.CloneCoefficient; break;
                case DerivedProcKind.AreaTick: k = _config.AreaTickCoefficient; break;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
            return k * sourceCoefficient;
        }

        /// <summary>
        /// Builds the context of a proc (trigger, Resonance, chain...) caused by the source.
        /// Caller must check <see cref="CanTrigger"/> first; Generation is Gen + 1.
        /// </summary>
        public ProcContext CreateProc(in ProcContext source, ulong sourceHash, HitFlags extraFlags = HitFlags.IsTriggered)
        {
            return new ProcContext(NextGeneration(source), source.Flags | extraFlags, source.SourceChain.Append(sourceHash),
                source.ProcCoefficient, source.AllowEchoRecursionOnce, source.EchoRecursionUsed);
        }

        /// <summary>
        /// Builds the replica context (flag IsEcho, Gen + 1, kind coefficient x source coefficient).
        /// Caller must check <see cref="CanReplicate"/> first. A replica of a replica marks the recursion as spent.
        /// </summary>
        public ProcContext CreateReplica(in ProcContext source, DerivedProcKind kind, ulong sourceHash)
        {
            return new ProcContext(NextGeneration(source), source.Flags | HitFlags.IsEcho,
                source.SourceChain.Append(sourceHash), DerivedProcCoefficient(kind, source.ProcCoefficient),
                source.AllowEchoRecursionOnce, source.EchoRecursionUsed || source.IsEcho);
        }
    }
}

using Eclipse.AI.Lod;
using Eclipse.AI.StateMachine;
using Eclipse.AI.Utility;

namespace Eclipse.Tests.EditMode.AI.Brain
{
    /// <summary>Fixtures: values here are test inputs, not balance data.</summary>
    internal static class BrainTestKit
    {
        public const float Dt = 1f / 60f;

        public static UtilityConfig MakeConfig(float randomChance, float fleeHp = 0f)
        {
            return new UtilityConfig
            {
                RandomChance = randomChance,
                FleeHpFraction = fleeHp,
                DesiredRange = 2f,
                ApproachFalloffDistance = 6f,
                CrowdAllyCount = 3,
                Attacks = new[]
                {
                    new AttackOption
                    {
                        MinRange = 0f, MaxRange = 2.5f, MaxAngleDeg = 60f, Weight = 1f,
                        DurationSeconds = 0.5f, RecoverSeconds = 0.4f
                    }
                }
            };
        }

        public static DecisionLodConfig MakeLod() => new DecisionLodConfig { NearRadius = 20f };

        public static AiPerception Percept(float distance, float angle = 0f, float hp = 1f, bool target = true,
            bool onScreen = true, float[] cooldowns = null)
        {
            return new AiPerception
            {
                HasTarget = target, OnScreen = onScreen, Distance = distance, AngleDeg = angle,
                HpFraction = hp, CooldownsRemaining = cooldowns
            };
        }

        public sealed class StubGate : ITokenGate
        {
            public bool Allow = true;
            public int Held;
            public int Acquired;
            public bool TryAcquire(int ownerId, bool ranged)
            {
                if (!Allow) return false;
                Held++; Acquired++;
                return true;
            }
            public void Release(int ownerId) { Held--; }
        }
    }
}

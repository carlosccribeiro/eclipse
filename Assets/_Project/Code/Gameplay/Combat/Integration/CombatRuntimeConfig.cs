using Eclipse.Core.Damage;
using Eclipse.Data.Combat;
using Eclipse.Gameplay.Player;
using UnityEngine;

namespace Eclipse.Gameplay.Combat.Integration
{
    /// <summary>
    /// Scene-level combat tuning that has no CSV yet (M05 T10). Everything the runtime needs to build a
    /// <see cref="CombatEntityConfig"/>, the offensive rules and the Flask comes from here, filled by
    /// <c>CombatRuntimeConfigCreator</c> with the values of the docs (section in each tooltip). Fields tagged
    /// "SANDBOX" are test-rig tuning that no doc defines (logged as a gap); they are not balance.
    /// </summary>
    public sealed class CombatRuntimeConfig : ScriptableObject
    {
        [Header("Data")]
        public CombatStatesDefinition combatStates;

        [Header("Incoming caps (01 3.4.1, 3.6)")]
        public float armorMitigationCap;
        public float physicalResistanceCap;
        public float resistanceCap;
        public float resistanceFloor;
        public float dodgeCap;
        public float blockCap;
        public float damageReductionCap;
        [Tooltip("Damage removed by a Block (06 3.3 step 4).")] public float blockAmount;
        [Tooltip("01 3.4.1: Normal difficulty = 0.")] public float difficultyResPenalty;
        [Tooltip("01 3.4.2: -0.20.")] public float resistanceFloorUnderStatus;

        [Header("Grace / death (06 3.14)")]
        public float graceSeconds;
        public float heavyGraceSeconds;
        public float heavyHitHealthFraction;
        public float tickIntervalSeconds;
        public float lowHealthFraction;
        public float deathSlowMotionScale;
        public float deathSlowMotionSeconds;
        public float reviveHealthFraction;
        public float reviveInvulnerabilitySeconds;
        public int maxRevivesPerBossRoom;

        [Header("Aether Flask (06 3.12)")]
        public int flaskCharges;
        public float flaskHealFraction;
        public float flaskHealSeconds;

        [Header("Kael level 1 (01 3.3)")]
        public float kaelMaxHealth;
        public float kaelArmor;
        public float kaelCriticalChance;
        public float kaelCriticalDamage;

        [Header("SANDBOX (gap: no doc defines these)")]
        public DifficultyTier difficulty = DifficultyTier.Normal;
        public ulong rngSeed = 20260101UL;
        [Tooltip("Monster level used as K of Armor for hits on the player.")] public float attackerLevel = 1f;
        public float dummyMaxHealth;
        public float dummyArmorPercent;
        [Tooltip("Seconds between telegraphed strikes of the test attacker (task brief: 2 s).")] public float testAttackIntervalSeconds;
        [Tooltip("Wind-up of the test strike (06 3.10: common enemy 0.6 s).")] public float testAttackWindupSeconds;
        public int testAttackActiveFrames;
        public int testAttackRecoveryFrames;
        public float testAttackRadius;
        public float testAttackArcDegrees;
        [Tooltip("Strike damage as a fraction of the target max health.")] public float testAttackHealthFraction;

        public MitigationRules BuildMitigation() => new MitigationRules(armorMitigationCap, physicalResistanceCap, resistanceCap,
            resistanceFloor, dodgeCap, blockCap, damageReductionCap);

        public OffensiveRules BuildOffensive() => new OffensiveRules(resistanceFloorUnderStatus);

        public CombatEntityConfig BuildEntityConfig() => new CombatEntityConfig
        {
            Mitigation = BuildMitigation(),
            BlockAmount = blockAmount,
            DifficultyResPenalty = difficultyResPenalty,
            GraceFrames = CombatEntityConfig.SecondsToFrames(graceSeconds),
            HeavyGraceFrames = CombatEntityConfig.SecondsToFrames(heavyGraceSeconds),
            HeavyHitHealthFraction = heavyHitHealthFraction,
            TickIntervalFrames = CombatEntityConfig.SecondsToFrames(tickIntervalSeconds),
            LowHealthFraction = lowHealthFraction,
            DeathSlowMotionScale = deathSlowMotionScale,
            DeathSlowMotionFrames = CombatEntityConfig.SecondsToFrames(deathSlowMotionSeconds),
            ReviveHealthFraction = reviveHealthFraction,
            ReviveInvulnerabilityFrames = CombatEntityConfig.SecondsToFrames(reviveInvulnerabilitySeconds),
            MaxRevivesPerBossRoom = maxRevivesPerBossRoom,
        };

        public AetherFlaskConfig BuildFlask() => new AetherFlaskConfig
        {
            MaxCharges = flaskCharges,
            HealFraction = flaskHealFraction,
            HealFrames = CombatEntityConfig.SecondsToFrames(flaskHealSeconds),
        };
    }
}

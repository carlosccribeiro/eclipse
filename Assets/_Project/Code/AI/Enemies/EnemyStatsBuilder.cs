using Eclipse.Data.Difficulties;
using Eclipse.Data.Enemies;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Entities;

namespace Eclipse.AI.Enemies
{
    /// <summary>Absolute stats of one spawned enemy.</summary>
    public readonly struct EnemyStats
    {
        public readonly float MaxHealth;
        /// <summary>Damage of a hit with <c>damage_rel = 1</c> (curve x enemy multiplier x difficulty multiplier).</summary>
        public readonly float Damage;

        public EnemyStats(float maxHealth, float damage)
        {
            MaxHealth = maxHealth;
            Damage = damage;
        }
    }

    /// <summary>
    /// Builds absolute stats from data (11 section 3.4.2, 06 section 3.13 R53): Standard enemy at the Monster Level
    /// (<see cref="EnemyScalingDefinition"/>) times the enemy multipliers times the difficulty multipliers. No number lives
    /// here; the mass-to-tier mapping is the structure of 06 section 3.8.1/3.8.2.
    /// </summary>
    public static class EnemyStatsBuilder
    {
        public static EnemyStats Build(EnemyDefinition enemy, in EnemyScalingEntry row, DifficultyDefinition difficulty)
        {
            float hpMult = difficulty != null ? difficulty.HpMult : 1f;
            float dmgMult = difficulty != null ? difficulty.DamageMult : 1f;
            return new EnemyStats(row.stdHp * enemy.HpMult * hpMult, row.stdDamage * enemy.DamageMult * dmgMult);
        }

        /// <summary>False when the Monster Level has no row in the curve.</summary>
        public static bool TryBuild(EnemyDefinition enemy, EnemyScalingDefinition scaling, int level,
            DifficultyDefinition difficulty, out EnemyStats stats)
        {
            if (scaling != null && scaling.TryGet(level, out EnemyScalingEntry row))
            {
                stats = Build(enemy, in row, difficulty);
                return true;
            }
            stats = default;
            return false;
        }

        /// <summary>
        /// Wind-up reduction applied at spawn. The difficulty asset stores the <b>cap</b> (06 section 3.10: "redução máxima em
        /// dificuldades altas"), and the table's wind-ups are the Normal values, so Normal applies none; higher tiers apply the cap
        /// (the per-tier schedule belongs to M20). Found by the T10 telegraph test: using the cap in Normal gave 0.595 s instead of 0.7 s.
        /// </summary>
        public static float WindupReduction(DifficultyDefinition difficulty)
        {
            if (difficulty == null || difficulty.Tier == Eclipse.Data.Difficulties.DifficultyTier.Normal) return 0f;
            return difficulty.MaxWindupReduction;
        }

        /// <summary>Damage of one attack: <c>damage_rel</c> times the enemy hit damage.</summary>
        public static float AttackDamage(in EnemyStats stats, AttackDefinition attack) => stats.Damage * attack.DamageRel;

        /// <summary>
        /// Stagger bar tier by mass (06 section 3.8.2): Light has no bar, Medium and Heavy have their own thresholds.
        /// Colossal maps to the Guardian bar; a boss overrides this at the controller (<c>isBoss</c>).
        /// </summary>
        public static StaggerTier TierFor(EnemyMassClass mass)
        {
            switch (mass)
            {
                case EnemyMassClass.Medium: return StaggerTier.Medium;
                case EnemyMassClass.Heavy: return StaggerTier.Heavy;
                case EnemyMassClass.Colossal: return StaggerTier.Guardian;
                default: return StaggerTier.None;
            }
        }

        public static MassClass ToGameplay(EnemyMassClass mass)
        {
            switch (mass)
            {
                case EnemyMassClass.Medium: return MassClass.Medium;
                case EnemyMassClass.Heavy: return MassClass.Heavy;
                case EnemyMassClass.Colossal: return MassClass.Colossal;
                default: return MassClass.Light;
            }
        }
    }
}

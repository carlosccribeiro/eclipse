namespace Eclipse.Core.Stats
{
    /// <summary>
    /// Every stat of GDD 6.3 / 01 sections 3.5-3.8 plus derived stats. Values are stable bit indices for
    /// <see cref="StatMask"/> (max 64); append only, never reorder.
    /// </summary>
    public enum StatId
    {
        // Primary
        Strength = 0, Dexterity, Intelligence,
        // Offensive
        AttackSpeed, CastSpeed, CriticalChance, CriticalDamage,
        PhysicalDamage, SkillDamage,
        EmberDamage, FrostDamage, StormDamage, VoidDamage, EchoDamage, RuinDamage,
        DamageOverTime, AreaDamage, ProjectileDamage, ArmorPenetration, StaggerDamage, StatusPotency, SummonDamage,
        // Defensive
        MaximumHealth, MaximumResource, Armor, PhysicalResistance, ElementalResistance, VoidResistance,
        DodgeChance, BlockChance, DamageReduction, HealthRegeneration, HealingEffectiveness, ShieldStrength,
        StatusResistance,
        // Utility
        MovementSpeed, CooldownReduction, ResourceGeneration, ResourceCostReduction, PickupRadius,
        ExperienceGain, GoldAcquisition, DurationOfEffects, SummonDuration,
        // General (all-damage) More/Increased bucket; home of Core Attunement (01 3.2.2, 06 3.2 step 4). Appended: bit indices are stable.
        GeneralDamage,
    }

    /// <summary>Helpers for <see cref="StatId"/>.</summary>
    public static class StatIds
    {
        /// <summary>Number of stats in the enum (also the exclusive upper bound of valid values).</summary>
        public const int Count = (int)StatId.GeneralDamage + 1;
    }
}

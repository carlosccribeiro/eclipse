
namespace Eclipse.Core.Damage
{
    /// <summary>
    /// Data-fed caps of the incoming pipeline (01 3.4.1, 3.6). Fractions (0.75 = 75%). The caller builds this from
    /// its <c>IStatRules</c>/data profile; nothing here is a balance number.
    /// </summary>
    public readonly struct MitigationRules
    {
        public MitigationRules(float armorMitigationCap, float physicalResistanceCap, float resistanceCap,
            float resistanceFloor, float dodgeCap, float blockCap, float damageReductionCap)
        {
            ArmorMitigationCap = armorMitigationCap;
            PhysicalResistanceCap = physicalResistanceCap;
            ResistanceCap = resistanceCap;
            ResistanceFloor = resistanceFloor;
            DodgeCap = dodgeCap;
            BlockCap = blockCap;
            DamageReductionCap = damageReductionCap;
        }

        /// <summary>Hard cap of Armor mitigation (01 3.4.1: 75%).</summary>
        public float ArmorMitigationCap { get; }
        /// <summary>Hard cap of Physical Resistance (01 3.4.1: 30%).</summary>
        public float PhysicalResistanceCap { get; }
        /// <summary>Cap of effective Elemental/Void resistance, applied AFTER the difficulty penalty (01 3.4.1: 75%).</summary>
        public float ResistanceCap { get; }
        /// <summary>Floor of effective resistance, negative (01 3.4.1: -50%).</summary>
        public float ResistanceFloor { get; }
        /// <summary>Hard cap of Dodge Chance (01 3.6: 40%).</summary>
        public float DodgeCap { get; }
        /// <summary>Hard cap of Block Chance (01 3.4.1: 50%).</summary>
        public float BlockCap { get; }
        /// <summary>Hard cap of Damage Reduction (01 3.4.1: 40%).</summary>
        public float DamageReductionCap { get; }
    }

    /// <summary>Pure mitigation formulas of 01 3.4.1 (player receiving damage). Allocation-free.</summary>
    public static class MitigationMath
    {
        /// <summary>K = KPerLevel x eLvl + KBase (01 3.4.1: 50 x eLvl + 500). Structural shape of the formula.</summary>
        public const float KPerLevel = 50f;
        public const float KBase = 500f;

        public static float ArmorConstant(float eLvl) => KPerLevel * eLvl + KBase;

        /// <summary>ArmorMit = Armor / (Armor + K), clamped to [0, cap] (01 3.4.1).</summary>
        public static float ArmorMitigation(float armor, float eLvl, float cap)
        {
            if (armor <= 0f) return 0f;
            float m = armor / (armor + ArmorConstant(eLvl));
            return m > cap ? cap : m;
        }

        public static float ArmorMitigation(float armor, float eLvl, in MitigationRules rules) =>
            ArmorMitigation(armor, eLvl, rules.ArmorMitigationCap);

        /// <summary>Two layers: 1 - (1 - ArmorMit) x (1 - PhysRes); PhysRes clamped to [0, cap] (01 3.4.1).</summary>
        public static float PhysicalMitigation(float armor, float eLvl, float physicalResistance, in MitigationRules rules)
        {
            float armorMit = ArmorMitigation(armor, eLvl, rules);
            float res = physicalResistance;
            if (res > rules.PhysicalResistanceCap) res = rules.PhysicalResistanceCap;
            if (res < 0f) res = 0f;
            return 1f - (1f - armorMit) * (1f - res);
        }

        /// <summary>min(raw - difficultyPenalty, cap), then floored at the minimum (01 3.4.1). May be negative.</summary>
        public static float EffectiveResistance(float rawResistance, float difficultyPenalty, in MitigationRules rules)
        {
            float r = rawResistance - difficultyPenalty;
            if (r > rules.ResistanceCap) r = rules.ResistanceCap;
            if (r < rules.ResistanceFloor) r = rules.ResistanceFloor;
            return r;
        }

        /// <summary>Mitigation fraction for a damage type of the player's defenses.</summary>
        public static float Mitigation(DamageType type, IncomingDefense defense, float eLvl, float difficultyPenalty,
            in MitigationRules rules)
        {
            switch (type.Family())
            {
                case MitigationFamily.Physical:
                    return PhysicalMitigation(defense.Armor, eLvl, defense.PhysicalResistance, rules);
                case MitigationFamily.Elemental:
                    return EffectiveResistance(defense.ElementalResistance, difficultyPenalty, rules);
                default:
                    return EffectiveResistance(defense.VoidResistance, difficultyPenalty, rules);
            }
        }

        /// <summary>EHP = health / (1 - mitigation) (01 3.12).</summary>
        public static float EffectiveHealth(float maxHealth, float mitigation)
        {
            if (mitigation >= 1f) return float.PositiveInfinity;
            return maxHealth / (1f - mitigation);
        }

        /// <summary>Physical EHP: health / (1 - MitPhysical) (01 3.12).</summary>
        public static float EffectiveHealthPhysical(float maxHealth, float armor, float eLvl, float physicalResistance,
            in MitigationRules rules) =>
            EffectiveHealth(maxHealth, PhysicalMitigation(armor, eLvl, physicalResistance, rules));

        /// <summary>Elemental/Void EHP with the difficulty penalty applied (01 3.12).</summary>
        public static float EffectiveHealthResistance(float maxHealth, float rawResistance, float difficultyPenalty,
            in MitigationRules rules) =>
            EffectiveHealth(maxHealth, EffectiveResistance(rawResistance, difficultyPenalty, rules));
    }
}

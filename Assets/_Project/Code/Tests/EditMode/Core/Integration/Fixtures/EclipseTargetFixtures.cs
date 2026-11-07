using Eclipse.Core.Damage;
using Eclipse.Core.Stats;

namespace Eclipse.Core.Integration.Tests
{
    /// <summary>Encounter side of the 01 3.12 example: eLvl 85 on Eclipse difficulty and the target's Armor%.</summary>
    internal static class EclipseTargetFixtures
    {
        public const float MonsterLevel = 85f;
        /// <summary>Eclipse resistance penalty (01 3.4.1): -25 pp.</summary>
        public const float EclipseResistancePenalty = 0.25f;

        // Hard caps of 01 3.4.1 / 3.6 (Armor mit 75%, Phys Res 30%, Res 75% after penalty, floor -50%, Dodge 40%, Block 50%, DR 40%).
        public static MitigationRules Mitigation() => new MitigationRules(0.75f, 0.30f, 0.75f, -0.50f, 0.40f, 0.50f, 0.40f);

        public static OffensiveRules Offensive() => new OffensiveRules(-0.20f);

        /// <summary>Training target of the Cleaving Strike line: Armor 30%.</summary>
        public static TargetDefense ArmoredTarget() => new TargetDefense { ArmorPercent = 0.30f };

        /// <summary>Kael's final defensive stats read from his StatBlock (capped by its IStatRules).</summary>
        public static IncomingDefense DefenseOf(StatBlock kael) => new IncomingDefense
        {
            Armor = kael.Evaluate(StatId.Armor),
            PhysicalResistance = kael.Evaluate(StatId.PhysicalResistance),
            ElementalResistance = kael.Evaluate(StatId.ElementalResistance),
            VoidResistance = kael.Evaluate(StatId.VoidResistance),
            DodgeChance = kael.Evaluate(StatId.DodgeChance),
            BlockChance = kael.Evaluate(StatId.BlockChance),
            DamageReduction = kael.Evaluate(StatId.DamageReduction),
        };
    }
}

using System;
using Eclipse.Data.Enemies;

namespace Eclipse.AI.Telegraphs
{
    /// <summary>Source class of an attack for wind-up floors (06 section 3.10).</summary>
    public enum WindupClass : byte { Common, Elite, Boss }

    /// <summary>
    /// Wind-up math of 06 section 3.10: effective = normal x (1 - reduction), reduction capped at 15%, never below the
    /// absolute floor of the class (common 0.45 s, Elite 0.55 s, boss 0.8 s), Annihilation at least 1.5 s, sniper line at least 0.8 s.
    /// The numbers are structural constants of the doc, not balance data.
    /// </summary>
    public static class WindupMath
    {
        /// <summary>06 section 3.10: maximum wind-up reduction by difficulty.</summary>
        public const float MaxReduction = 0.15f;
        /// <summary>06 section 3.10 floor table, common (includes fodder).</summary>
        public const float FloorCommon = 0.45f;
        /// <summary>06 section 3.10 floor table, Elite.</summary>
        public const float FloorElite = 0.55f;
        /// <summary>06 section 3.10 floor table, boss heavy strike.</summary>
        public const float FloorBoss = 0.8f;
        /// <summary>06 section 3.10: Annihilation is always telegraphed for at least 1.5 s.</summary>
        public const float FloorAnnihilation = 1.5f;
        /// <summary>06 section 3.10: sniper line telegraph, 0.8 s minimum.</summary>
        public const float FloorSniper = 0.8f;
        /// <summary>Tolerance for float comparisons against the floors.</summary>
        public const float Epsilon = 1e-4f;

        public static float ClassFloor(WindupClass windupClass)
        {
            switch (windupClass)
            {
                case WindupClass.Elite: return FloorElite;
                case WindupClass.Boss: return FloorBoss;
                default: return FloorCommon;
            }
        }

        /// <summary>Absolute floor of one attack: the highest of its class floor and the special floors.</summary>
        public static float Floor(WindupClass windupClass, bool annihilation, bool sniper)
        {
            float floor = ClassFloor(windupClass);
            if (annihilation) floor = Math.Max(floor, FloorAnnihilation);
            if (sniper) floor = Math.Max(floor, FloorSniper);
            return floor;
        }

        /// <summary>Wind-up after the (capped) difficulty reduction, before applying any floor.</summary>
        public static float Reduced(float normalSeconds, float reduction)
        {
            float r = reduction < 0f ? 0f : (reduction > MaxReduction ? MaxReduction : reduction);
            return normalSeconds * (1f - r);
        }

        /// <summary>Runtime wind-up: reduced and clamped to the floor.</summary>
        public static float Effective(float normalSeconds, float reduction, WindupClass windupClass, bool annihilation = false, bool sniper = false)
        {
            return Math.Max(Reduced(normalSeconds, reduction), Floor(windupClass, annihilation, sniper));
        }

        /// <summary>A sniper line is a Line telegraph of a non-melee (projectile/lobbed) attack (06 section 3.10 "Linha (sniper)").</summary>
        public static bool IsSniper(TelegraphShape shape, AttackDelivery delivery)
        {
            return shape == TelegraphShape.Line && (delivery == AttackDelivery.Projectile || delivery == AttackDelivery.Lobbed);
        }
    }
}

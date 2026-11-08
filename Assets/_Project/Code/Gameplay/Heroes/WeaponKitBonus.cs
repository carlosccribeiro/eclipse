using System;

namespace Eclipse.Gameplay.Heroes
{
    /// <summary>
    /// Kit bonus granted by the equipped weapon (08 section 4.6). Plain data: skills and the Fury/Parry/execute code
    /// read the field they care about and apply it themselves. Zero means "no bonus of this kind".
    /// Percentages are fractions (0.20 = +20%).
    /// </summary>
    public readonly struct WeaponKitBonus
    {
        /// <summary>Greatsword: Cleaving Strike +20% area. Apply with <see cref="ScaleRadius"/>.</summary>
        public readonly float CleavingStrikeAreaIncrease;
        /// <summary>Dual Axes: extra Fury per Basic attack (+2).</summary>
        public readonly float FuryPerBasic;
        /// <summary>Dual Axes: extra Burning Chains chains (+1).</summary>
        public readonly int BurningChainsExtraChains;
        /// <summary>Warhammer: Stagger Damage increase for Infernal Leap and Worldbreaker (+25%).</summary>
        public readonly float LeapWorldbreakerStaggerDamageIncrease;
        /// <summary>Warhammer: Infernal Leap radius bonus in meters (+1 m, flat).</summary>
        public readonly float LeapRadiusBonusMeters;
        /// <summary>Longsword: Ashen Guard Parry window bonus in seconds (+0.1 s).</summary>
        public readonly float ParryWindowBonusSeconds;
        /// <summary>Longsword: counter-attack damage increase (+20%).</summary>
        public readonly float CounterAttackDamageIncrease;
        /// <summary>Heavy Blade: percentage points added to every Kael execute threshold, as a fraction (0.05).</summary>
        public readonly float ExecuteThresholdBonus;

        public WeaponKitBonus(float cleavingStrikeAreaIncrease = 0f, float furyPerBasic = 0f, int burningChainsExtraChains = 0,
            float leapWorldbreakerStaggerDamageIncrease = 0f, float leapRadiusBonusMeters = 0f,
            float parryWindowBonusSeconds = 0f, float counterAttackDamageIncrease = 0f, float executeThresholdBonus = 0f)
        {
            CleavingStrikeAreaIncrease = cleavingStrikeAreaIncrease;
            FuryPerBasic = furyPerBasic;
            BurningChainsExtraChains = burningChainsExtraChains;
            LeapWorldbreakerStaggerDamageIncrease = leapWorldbreakerStaggerDamageIncrease;
            LeapRadiusBonusMeters = leapRadiusBonusMeters;
            ParryWindowBonusSeconds = parryWindowBonusSeconds;
            CounterAttackDamageIncrease = counterAttackDamageIncrease;
            ExecuteThresholdBonus = executeThresholdBonus;
        }

        // Structural numbers from the docs (08 section 4.6, 00 R08/R20, 06 section 3.2).
        /// <summary>Parry window hard cap in seconds (00 R20).</summary>
        public const float ParryWindowCapSeconds = 0.3f;
        /// <summary>Execute threshold hard cap as a fraction (08 section 4.6, 00 R08).</summary>
        public const float ExecuteThresholdCap = 0.20f;
        /// <summary>Elites receive half of the execute threshold bonus (08 section 4.6, 00 R08).</summary>
        public const float EliteExecuteBonusFactor = 0.5f;
        /// <summary>Heavy Blade Heavy: health fraction below which the bonus applies (08 section 4.6).</summary>
        public const float HeavyBladeLowHealthThreshold = 0.35f;
        /// <summary>Heavy Blade Heavy: extra damage against low-health targets, as a fraction (+100%). Not an Execute.</summary>
        public const float HeavyBladeLowHealthDamageBonus = 1.0f;

        public static readonly WeaponKitBonus None = default;

        /// <summary>Kit bonus of a Kael weapon by its StableId; <see cref="None"/> for an unknown weapon.</summary>
        public static WeaponKitBonus ForWeapon(string weaponId)
        {
            switch (weaponId)
            {
                case "weapon.kael.greatsword": return new WeaponKitBonus(cleavingStrikeAreaIncrease: 0.20f);
                case "weapon.kael.dual_axes": return new WeaponKitBonus(furyPerBasic: 2f, burningChainsExtraChains: 1);
                case "weapon.kael.warhammer":
                    return new WeaponKitBonus(leapWorldbreakerStaggerDamageIncrease: 0.25f, leapRadiusBonusMeters: 1f);
                case "weapon.kael.longsword":
                    return new WeaponKitBonus(parryWindowBonusSeconds: 0.1f, counterAttackDamageIncrease: 0.20f);
                case "weapon.kael.heavy_blade": return new WeaponKitBonus(executeThresholdBonus: 0.05f);
                default: return None;
            }
        }

        /// <summary>Area increase X multiplies the radius by sqrt(1 + X) (06 section 3.2).</summary>
        public static float ScaleRadius(float radius, float areaIncrease) => radius * (float)Math.Sqrt(1.0 + areaIncrease);

        /// <summary>Parry window with the Longsword bonus, clamped to the 0.3 s cap (00 R20).</summary>
        public float ApplyParryWindow(float baseSeconds) =>
            Math.Min(baseSeconds + ParryWindowBonusSeconds, Math.Max(baseSeconds, ParryWindowCapSeconds));

        /// <summary>
        /// Execute threshold (fraction) with the Heavy Blade bonus, never above 0.20 (00 R08). Elites get half the bonus.
        /// A base threshold already above the cap is returned unchanged.
        /// </summary>
        public float ApplyExecuteThreshold(float baseThreshold, bool targetIsElite)
        {
            float bonus = targetIsElite ? ExecuteThresholdBonus * EliteExecuteBonusFactor : ExecuteThresholdBonus;
            return Math.Min(baseThreshold + bonus, Math.Max(baseThreshold, ExecuteThresholdCap));
        }

        /// <summary>Heavy Blade Heavy damage multiplier: x2 against targets below 35% health (08 section 4.6).</summary>
        public static float HeavyBladeHeavyMultiplier(float targetHealthFraction) =>
            targetHealthFraction < HeavyBladeLowHealthThreshold ? 1f + HeavyBladeLowHealthDamageBonus : 1f;
    }
}

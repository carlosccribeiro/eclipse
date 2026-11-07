using System;

namespace Eclipse.Core.Attributes
{
    /// <summary>A value that grows linearly per level: Base at level 1 plus PerLevel for each further level (01 3.3).</summary>
    public readonly struct LevelScaling
    {
        public LevelScaling(float baseValue, float perLevel)
        {
            Base = baseValue;
            PerLevel = perLevel;
        }

        /// <summary>Value at level 1.</summary>
        public float Base { get; }
        /// <summary>Increment applied from level 2 on.</summary>
        public float PerLevel { get; }

        public float ValueAt(int level) => HeroLevelScaling.Value(Base, PerLevel, level);
    }

    /// <summary>Per-level growth of hero values (01 3.2.3, 3.3).</summary>
    public static class HeroLevelScaling
    {
        /// <summary>01 3.3: <c>Value(lv) = Base + (lv - 1) * PerLevel</c>. Levels below 1 count as 1.</summary>
        public static float Value(float baseValue, float perLevel, int level)
        {
            int steps = Math.Max(level, 1) - 1;
            return baseValue + steps * perLevel;
        }
    }
}

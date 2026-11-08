using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Combat
{
    /// <summary>
    /// Impact feedback per weapon weight class (06 section 3.11): crit hit-stop bonus, hit-stop spacing,
    /// screen-shake trauma, target flash and squash. Source: HitFeedback.csv. Per-hit hit-stop lives in <see cref="AttackStep"/>.
    /// </summary>
    public sealed class HitFeedbackProfile : Definition
    {
        [SerializeField] private WeaponWeightClass weightClass;
        [SerializeField] private int critHitStopBonusFrames;
        [SerializeField] private int minHitStopSpacingMs;
        [SerializeField] private float shakeTrauma;
        [SerializeField] private float shakeTraumaCrit;
        [SerializeField] private float traumaDecayPerSecond;
        [SerializeField] private int flashFrames;
        [SerializeField] private float squashSeconds;

        public WeaponWeightClass WeightClass => weightClass;
        /// <summary>Extra hit-stop frames on a critical hit.</summary>
        public int CritHitStopBonusFrames => critHitStopBonusFrames;
        /// <summary>At most one attacker hit-stop per this many milliseconds.</summary>
        public int MinHitStopSpacingMs => minHitStopSpacingMs;
        /// <summary>Trauma added on a hit (shake = trauma squared); 0 = no shake.</summary>
        public float ShakeTrauma => shakeTrauma;
        public float ShakeTraumaCrit => shakeTraumaCrit;
        public float TraumaDecayPerSecond => traumaDecayPerSecond;
        /// <summary>White flash on the target, in frames.</summary>
        public int FlashFrames => flashFrames;
        /// <summary>Visual push (squash) duration in seconds; 0 = none.</summary>
        public float SquashSeconds => squashSeconds;

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(WeaponWeightClass weight, int critBonus, int spacingMs, float trauma, float traumaCrit,
            float decay, int flash, float squash)
        {
            weightClass = weight;
            critHitStopBonusFrames = critBonus;
            minHitStopSpacingMs = spacingMs;
            shakeTrauma = trauma;
            shakeTraumaCrit = traumaCrit;
            traumaDecayPerSecond = decay;
            flashFrames = flash;
            squashSeconds = squash;
        }
#endif
    }
}

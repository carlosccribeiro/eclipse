using System;
using System.Collections.Generic;
using Eclipse.AI.Telegraphs;
using Eclipse.Core.Tags;
using Eclipse.Data.Difficulties;
using Eclipse.Data.Enemies;
using UnityEditor;

namespace Eclipse.Editor.Validation
{
    /// <summary>
    /// Telegraph rules of 06 section 3.10 (R14): effective wind-up (normal x (1 - reduction)) in every difficulty must not fall
    /// below the floor of the attack class (derived from the owner enemy loot class: Elite/Guardian = Elite, Boss = Boss, else common);
    /// Annihilation at least 1.5 s; sniper line (Line + projectile/lobbed) at least 0.8 s; no orange telegraph color.
    /// Missing difficulty definitions are covered by testing the 15% cap.
    /// </summary>
    [ValidationRule]
    public sealed class TelegraphWindupRule : IValidationRule
    {
        /// <summary>Palette used for the orange check; tests may replace it. Default: the project palette asset.</summary>
        public static Func<TelegraphPalette> PaletteProvider = () =>
            AssetDatabase.LoadAssetAtPath<TelegraphPalette>(Setup.TelegraphPaletteCreator.AssetPath);

        public string Name => "TelegraphWindup";

        public void Validate(ValidationContext context)
        {
            var difficulties = new List<DifficultyDefinition>();
            var classOf = new Dictionary<AttackDefinition, WindupClass>();
            foreach (var d in context.Definitions)
            {
                if (d is DifficultyDefinition diff) difficulties.Add(diff);
                else if (d is EnemyDefinition enemy)
                {
                    WindupClass c = ClassOf(enemy);
                    foreach (AttackDefinition a in enemy.Attacks) Assign(classOf, a, c);
                }
            }

            TelegraphPalette palette = PaletteProvider != null ? PaletteProvider() : null;

            foreach (var d in context.Definitions)
            {
                if (!(d is AttackDefinition attack)) continue;
                WindupClass cls = classOf.TryGetValue(attack, out WindupClass found) ? found : WindupClass.Common;
                bool annihilation = attack.TelegraphType == TelegraphType.Annihilation || (attack.HitFlags & HitFlags.Annihilation) != 0;
                bool sniper = WindupMath.IsSniper(attack.TelegraphShape, attack.Delivery);
                float floor = WindupMath.Floor(cls, annihilation, sniper);

                if (attack.WindupSeconds <= 0f && attack.Trigger == AttackTrigger.OnDeath && !annihilation) continue;

                if (difficulties.Count == 0)
                {
                    Check(context, attack, "no difficulty (15% cap)", WindupMath.MaxReduction, floor, cls, annihilation, sniper);
                }
                else
                {
                    foreach (var diff in difficulties)
                        Check(context, attack, diff.StableIdString, diff.MaxWindupReduction, floor, cls, annihilation, sniper);
                }

                if (palette != null) CheckColors(context, attack, palette, sniper);
            }
        }

        private void Check(ValidationContext context, AttackDefinition attack, string difficulty, float reduction, float floor,
            WindupClass cls, bool annihilation, bool sniper)
        {
            float effective = WindupMath.Reduced(attack.WindupSeconds, reduction);
            if (effective < floor - WindupMath.Epsilon)
            {
                string why = annihilation ? "Annihilation" : (sniper ? "sniper" : cls.ToString());
                context.Report.Error(Name, attack, "effective wind-up " + effective.ToString("0.###") + " s in " + difficulty
                    + " is below the " + why + " floor of " + floor.ToString("0.###") + " s (windup_s " + attack.WindupSeconds.ToString("0.###") + ")");
            }
        }

        private void CheckColors(ValidationContext context, AttackDefinition attack, TelegraphPalette palette, bool sniper)
        {
            TelegraphVisual visual = sniper ? TelegraphVisual.SniperLine : TelegraphVisuals.From(attack.TelegraphType);
            if (!palette.TryGet(visual, out TelegraphPalette.Entry e))
            {
                context.Report.Error(Name, attack, "telegraph " + visual + " has no palette entry");
                return;
            }
            if (TelegraphPalette.IsOrange(e.fill) || TelegraphPalette.IsOrange(e.border))
                context.Report.Error(Name, attack, "telegraph " + visual + " uses an orange color (reserved to Ember, 06 section 3.10)");
        }

        private static WindupClass ClassOf(EnemyDefinition enemy)
        {
            switch (enemy.LootClass)
            {
                case EnemyLootClass.Elite:
                case EnemyLootClass.Guardian: return WindupClass.Elite;
                case EnemyLootClass.Boss: return WindupClass.Boss;
                default: return WindupClass.Common;
            }
        }

        private static void Assign(Dictionary<AttackDefinition, WindupClass> map, AttackDefinition attack, WindupClass cls)
        {
            // Follow combo chains; keep the strictest (highest) class if shared.
            int guard = 0;
            while (attack != null && guard++ < 16)
            {
                if (!map.TryGetValue(attack, out WindupClass existing) || cls > existing) map[attack] = cls;
                attack = attack.ChainNext;
            }
        }
    }
}

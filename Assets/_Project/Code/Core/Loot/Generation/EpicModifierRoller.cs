using System.Collections.Generic;
using Eclipse.Core.Items;
using Eclipse.Core.Random;

namespace Eclipse.Core.Loot.Generation
{
    /// <summary>
    /// Step 9 (Epic) and step 10 of 05 section 6.2 / 05 section 7.2: the Epic modifier. With probability EpicHeroModifierShare (75%)
    /// it comes from the target hero pool, weight x EquippedSkillMultiplier (x3) when the target skill is equipped, and the item becomes
    /// hero-bound (R27); otherwise a universal GEN-xx (never bound). An empty branch falls back to the other one.
    /// Not thread-safe (reusable buffers).
    /// </summary>
    public sealed class EpicModifierRoller
    {
        private readonly ILootCatalog _loot;
        private readonly IModifierPoolProvider _mods;
        private readonly List<string> _equipped = new List<string>(8);
        private float[] _w = new float[32];

        public EpicModifierRoller(ILootCatalog loot, IModifierPoolProvider mods) { _loot = loot; _mods = mods; }

        public void Roll(ItemInstance item, string hero, EquipSlot slot, LootContext ctx, int count, float rollMin, ref Xoshiro128StarStar rng)
        {
            // Copy once: iterating the interface collection per candidate would allocate an enumerator each time.
            _equipped.Clear();
            if (ctx.EquippedSkillIds != null) foreach (var s in ctx.EquippedSkillIds) _equipped.Add(s);

            var smart = _loot.SmartWeights;
            for (int c = 0; c < count; c++)
            {
                bool heroBranch = rng.Chance(smart.EpicHeroModifierShare);
                var heroList = _mods.GetHeroModifiers(hero, slot);
                var genList = _mods.GetUniversalModifiers(hero, slot);
                bool useHero = heroBranch;
                if (useHero && (heroList == null || heroList.Count == 0)) useHero = false;
                else if (!useHero && (genList == null || genList.Count == 0) && heroList != null && heroList.Count > 0) useHero = true;
                var list = useHero ? heroList : genList;
                if (list == null || list.Count == 0) return;

                Sampling.Ensure(ref _w, list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    var cand = list[i];
                    float w = cand.Weight;
                    if (useHero && cand.SkillId != null && _equipped.Contains(cand.SkillId)) w *= smart.EquippedSkillMultiplier;
                    if (HasModifier(item, cand.ModifierId)) w = 0f;
                    _w[i] = w;
                }
                int idx = Sampling.PickWeighted(_w, list.Count, ref rng);
                if (idx < 0) return;
                item.Modifiers.Add(new RolledModifier { ModifierId = list[idx].ModifierId, Roll01 = Sampling.RollQuality(ref rng, rollMin) });
                if (useHero) item.BoundHeroId = hero; // R27
            }
        }

        private static bool HasModifier(ItemInstance item, string id)
        {
            for (int i = 0; i < item.Modifiers.Count; i++) if (item.Modifiers[i].ModifierId == id) return true;
            return false;
        }
    }
}

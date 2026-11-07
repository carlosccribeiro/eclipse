using Eclipse.Core.Items;
using Eclipse.Core.Random;

namespace Eclipse.Core.Loot.Generation
{
    /// <summary>
    /// Step 5 of 05 section 6.2 for Common/Rare/Epic: slot by weight (05 section 6.1; Ring and Relic are one draw each, Off Hand always drawable, R29),
    /// then a base of the slot uniformly among the droppable bases compatible with the hero (02 section 3.10).
    /// Slots without any compatible base get weight 0. Not thread-safe (reusable buffers).
    /// </summary>
    public sealed class BaseSelector
    {
        private const int SlotCount = 10;
        private readonly IItemCatalog _items;
        private readonly ILootCatalog _loot;
        private readonly float[] _slotWeights = new float[SlotCount];
        private ItemBaseData[] _candidates = new ItemBaseData[32];

        public BaseSelector(IItemCatalog items, ILootCatalog loot) { _items = items; _loot = loot; }

        public static bool Compatible(ItemBaseData b, string hero) => b.Droppable && (string.IsNullOrEmpty(b.HeroId) || b.HeroId == hero);

        /// <summary>Returns the chosen base, or null when no slot has a compatible base.</summary>
        public ItemBaseData Pick(string hero, ref Xoshiro128StarStar rng)
        {
            var bases = _items.Bases;
            var weights = _loot.SlotWeights.Weights;
            for (int s = 0; s < SlotCount; s++) _slotWeights[s] = 0f;
            for (int i = 0; i < bases.Count; i++)
            {
                if (!Compatible(bases[i], hero)) continue;
                int s = (int)bases[i].Slot;
                _slotWeights[s] = s < weights.Length ? weights[s] : 0f;
            }
            int slot = Sampling.PickWeighted(_slotWeights, SlotCount, ref rng);
            if (slot < 0) return null;

            Sampling.Ensure(ref _candidates, bases.Count);
            int n = 0;
            for (int i = 0; i < bases.Count; i++)
                if ((int)bases[i].Slot == slot && Compatible(bases[i], hero)) _candidates[n++] = bases[i];
            return _candidates[rng.Range(0, n)];
        }
    }
}

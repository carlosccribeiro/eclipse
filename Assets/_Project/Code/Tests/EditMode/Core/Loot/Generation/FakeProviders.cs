using System.Collections.Generic;
using Eclipse.Core.Items;

namespace Eclipse.Core.Loot.Generation.Tests
{
    public sealed class FakeModifierProvider : IModifierPoolProvider
    {
        public readonly Dictionary<string, List<ModifierCandidate>> HeroMods = new Dictionary<string, List<ModifierCandidate>>();
        public readonly List<ModifierCandidate> Universal = new List<ModifierCandidate>();
        /// <summary>Affix ids not relevant to the hero skills (x0.25).</summary>
        public readonly HashSet<string> IrrelevantAffixIds = new HashSet<string>();
        /// <summary>Primary role by affix id (default Core).</summary>
        public readonly Dictionary<string, PrimaryRole> Roles = new Dictionary<string, PrimaryRole>();

        public IReadOnlyList<ModifierCandidate> GetHeroModifiers(string heroId, EquipSlot slot)
            => HeroMods.TryGetValue(heroId ?? string.Empty, out var l) ? l : new List<ModifierCandidate>();
        public IReadOnlyList<ModifierCandidate> GetUniversalModifiers(string heroId, EquipSlot slot) => Universal;
        public bool IsAffixRelevant(string heroId, AffixData affix) => !IrrelevantAffixIds.Contains(affix.Id);
        public PrimaryRole GetPrimaryRole(string heroId, AffixData primaryAffix)
            => Roles.TryGetValue(primaryAffix.Id, out var r) ? r : PrimaryRole.Core;
    }

    public sealed class FakeCollection : IItemCollection
    {
        public readonly HashSet<string> Owned = new HashSet<string>();
        public bool Owns(string definitionId) => Owned.Contains(definitionId);
    }

    public sealed class FakePity : IPityState
    {
        public int Drought { get; set; }
        public int LegendariesWithoutSet { get; set; }
        public int Calls;
        public Rarity LastRarity;
        public ItemKind LastKind;
        public bool LastConsumes;
        public int KillsWithoutUnique(string bossId) => 0;
        public bool HasFirstKill(string bossId, Difficulty difficulty) => false;
        public bool HasClearedRegionOnNormal(string regionId) => false;
        public void OnItemGenerated(Rarity rarity, ItemKind kind, bool consumesHardPity)
        {
            Calls++; LastRarity = rarity; LastKind = kind; LastConsumes = consumesHardPity;
        }
    }
}

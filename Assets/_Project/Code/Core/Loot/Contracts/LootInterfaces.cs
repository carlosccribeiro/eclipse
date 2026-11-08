using System.Collections.Generic;
using Eclipse.Core.Items;
using Eclipse.Core.Random;

namespace Eclipse.Core.Loot
{
    /// <summary>One candidate skill modifier from 03 for Epic items (the real source arrives with M10/M13).</summary>
    public readonly struct ModifierCandidate
    {
        public readonly string ModifierId;
        /// <summary>Skill the modifier targets; null for universal <c>GEN-xx</c>.</summary>
        public readonly string SkillId;
        public readonly float Weight;
        public ModifierCandidate(string modifierId, string skillId, float weight) { ModifierId = modifierId; SkillId = skillId; Weight = weight; }
    }

    /// <summary>Source of Epic modifier candidates (fake in tests; real in M10/M13). Must return deterministic order.</summary>
    public interface IModifierPoolProvider
    {
        /// <summary>Hero-specific modifiers compatible with the slot (75% branch).</summary>
        IReadOnlyList<ModifierCandidate> GetHeroModifiers(string heroId, EquipSlot slot);
        /// <summary>Universal <c>GEN-xx</c> compatible with the tags of some skill of the hero (25% branch).</summary>
        IReadOnlyList<ModifierCandidate> GetUniversalModifiers(string heroId, EquipSlot slot);
        /// <summary>True when the hero's skill tags include the tags the affix cares about (smart loot relevance, 05 section 6.2).</summary>
        bool IsAffixRelevant(string heroId, AffixData affix);
        /// <summary>Primary-attribute role of the hero (Core x2 / Secondary x1 / Tertiary x0.5; R30).</summary>
        PrimaryRole GetPrimaryRole(string heroId, AffixData primaryAffix);
    }

    /// <summary>Role of a primary attribute for a hero (01).</summary>
    public enum PrimaryRole : byte { Core, Secondary, Tertiary }

    /// <summary>Per-hero pity read/write view (05 section 8). The POCO implementation lives in Loot/Pity.</summary>
    public interface IPityState
    {
        /// <summary>Items received since the last compatible Legendary.</summary>
        int Drought { get; }
        /// <summary>Consecutive compatible Legendaries without a set piece.</summary>
        int LegendariesWithoutSet { get; }
        int KillsWithoutUnique(string bossId);
        bool HasFirstKill(string bossId, Difficulty difficulty);
        bool HasClearedRegionOnNormal(string regionId);
        /// <summary>Called by the generator after each item so counters advance.</summary>
        void OnItemGenerated(Rarity rarity, ItemKind kind, bool consumesHardPity);
    }

    /// <summary>Ownership lookup in any quality: equipped, inventory or Vault (05 section 8.3).</summary>
    public interface IItemCollection
    {
        bool Owns(string definitionId);
    }

    /// <summary>Entry point contract: (LootContext, rng) -> item (11 section 3.12).</summary>
    public interface IItemGenerator
    {
        ItemInstance Generate(LootContext context, ref Xoshiro128StarStar rng);
    }
}

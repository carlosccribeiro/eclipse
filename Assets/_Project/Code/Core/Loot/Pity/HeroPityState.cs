using System.Collections.Generic;
using Eclipse.Core.Items;

namespace Eclipse.Core.Loot.Pity
{
    /// <summary>Kills of one boss since this hero last got one of its exclusive Uniques (05 section 8.5).</summary>
    public sealed class BossKillCounter
    {
        public string BossId { get; set; }
        public int Kills { get; set; }
    }

    /// <summary>A (boss, difficulty) pair already killed once by this hero (05 section 8.4).</summary>
    public sealed class FirstKillRecord
    {
        public string BossId { get; set; }
        public Difficulty Difficulty { get; set; }
    }

    /// <summary>
    /// Per-hero pity counters (05 section 8; persisted by M18). Plain serializable data: simple properties and lists of pairs,
    /// no references to ScriptableObjects or runtime services. The drought is kept across difficulty changes (05 section 8.1);
    /// thresholds come from the difficulty of the next drop.
    /// A Legendary generated through this state counts as "compatible" (the caller owns one state per hero; the
    /// <see cref="IPityState.OnItemGenerated"/> contract carries no hero).
    /// </summary>
    public sealed class HeroPityState : IPityState
    {
        public string HeroId { get; set; }
        /// <summary>Items received since the last compatible Legendary (every source counts, trash included).</summary>
        public int Drought { get; set; }
        /// <summary>Consecutive compatible Legendaries without a set piece.</summary>
        public int LegendariesWithoutSet { get; set; }
        public List<BossKillCounter> BossKills { get; set; } = new List<BossKillCounter>();
        public List<FirstKillRecord> FirstKills { get; set; } = new List<FirstKillRecord>();
        /// <summary>Regions whose first clear on Normal was already rewarded.</summary>
        public List<string> ClearedRegionsOnNormal { get; set; } = new List<string>();

        public int KillsWithoutUnique(string bossId)
        {
            var c = Find(bossId);
            return c == null ? 0 : c.Kills;
        }

        public bool HasFirstKill(string bossId, Difficulty difficulty)
        {
            for (int i = 0; i < FirstKills.Count; i++)
                if (FirstKills[i].Difficulty == difficulty && FirstKills[i].BossId == bossId) return true;
            return false;
        }

        public bool HasClearedRegionOnNormal(string regionId) => ClearedRegionsOnNormal.Contains(regionId);

        /// <summary>Advances the counters after each generated item (rarity/kind of the final item, not the rolled one).</summary>
        public void OnItemGenerated(Rarity rarity, ItemKind kind, bool consumesHardPity)
        {
            if (rarity == Rarity.Legendary)
            {
                Drought = 0;
                LegendariesWithoutSet = kind == ItemKind.SetPiece ? 0 : LegendariesWithoutSet + 1;
            }
            else Drought++;
        }

        /// <summary>Call once per boss kill, after its drops: counts the kill, resets the counter if a boss Unique dropped, records the first kill.</summary>
        public void RegisterBossKill(string bossId, Difficulty difficulty, bool gotBossUnique)
        {
            var c = Find(bossId);
            if (c == null) { c = new BossKillCounter { BossId = bossId }; BossKills.Add(c); }
            c.Kills = gotBossUnique ? 0 : c.Kills + 1;
            if (!HasFirstKill(bossId, difficulty)) FirstKills.Add(new FirstKillRecord { BossId = bossId, Difficulty = difficulty });
        }

        /// <summary>Call after the first-clear Rift Cache of a region on Normal was granted.</summary>
        public void MarkRegionClearedOnNormal(string regionId)
        {
            if (!ClearedRegionsOnNormal.Contains(regionId)) ClearedRegionsOnNormal.Add(regionId);
        }

        private BossKillCounter Find(string bossId)
        {
            for (int i = 0; i < BossKills.Count; i++) if (BossKills[i].BossId == bossId) return BossKills[i];
            return null;
        }
    }
}

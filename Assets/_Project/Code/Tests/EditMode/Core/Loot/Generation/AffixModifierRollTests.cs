using System.Collections.Generic;
using Eclipse.Core.Items;
using Eclipse.Core.Random;
using Eclipse.Core.Tags;
using NUnit.Framework;

namespace Eclipse.Core.Loot.Generation.Tests
{
    [TestFixture]
    public class AffixRollerTests
    {
        private GenFixture _f;
        [SetUp] public void SetUp() => _f = GenFixture.Create();

        private static AffixData A(string id, AffixGroup g = AffixGroup.None, AffixCategory c = AffixCategory.Offensive, TagMask? relevance = null, TagMask? affinity = null)
        {
            var a = new AffixData { Id = id, Group = g, Category = c, RelevanceTags = relevance ?? TagMask.None, Affinity = affinity ?? TagMask.None };
            for (int t = 0; t < 5; t++) a.Tiers[t] = new TierRange(0.1f, 0.2f);
            return a;
        }

        /// <summary>Replaces the catalog pool with the given (affix, weight at Head) entries.</summary>
        private void Pool(EquipSlot slot, params (AffixData a, float w)[] entries)
        {
            _f.Items.AffixList.Clear(); _f.Items.PoolList.Clear();
            foreach (var (a, w) in entries)
            {
                _f.Items.AffixList.Add(a);
                var e = new AffixPoolEntry { AffixId = a.Id };
                e.SlotWeights[(int)slot] = w;
                _f.Items.PoolList.Add(e);
            }
        }

        private AffixRoller Roller(GeneratorRules rules = null) => new AffixRoller(_f.Items, _f.Loot, _f.Mods, rules);

        private static ItemInstance Item(Rarity r) => new ItemInstance { Rarity = r };

        private Dictionary<string, int> Count(AffixRoller roller, EquipSlot slot, string hero, int count, int n, Rarity r = Rarity.Rare, ItemBaseData b = null)
        {
            var rng = GenFixture.Rng(77); var d = new Dictionary<string, int>();
            for (int i = 0; i < n; i++)
            {
                var item = Item(r);
                roller.RollRandom(item, b, slot, hero, count, 3, 0f, ref rng);
                foreach (var a in item.Affixes) d[a.AffixId] = d.TryGetValue(a.AffixId, out var v) ? v + 1 : 1;
            }
            return d;
        }

        [Test]
        public void IrrelevantAffix_WeightX025()
        {
            Pool(EquipSlot.Head, (A("a", relevance: new TagMask(Tag.Attack)), 10), (A("b", relevance: new TagMask(Tag.Summon)), 10));
            _f.Mods.IrrelevantAffixIds.Add("b");
            var d = Count(Roller(), EquipSlot.Head, "kael", 1, 20000);
            Assert.AreEqual(0.8, d["a"] / 20000.0, 0.01);
        }

        [Test]
        public void AffixWithoutRelevanceTags_IsAlwaysRelevant()
        {
            Pool(EquipSlot.Head, (A("a"), 10), (A("b"), 10));
            _f.Mods.IrrelevantAffixIds.Add("b"); // would be x0.25 if it had tags
            var d = Count(Roller(), EquipSlot.Head, "kael", 1, 20000);
            Assert.AreEqual(0.5, d["a"] / 20000.0, 0.012);
        }

        [Test]
        public void PrimaryRoles_Core2_Secondary1_Tertiary05()
        {
            Pool(EquipSlot.Head, (A("str", AffixGroup.Primary, AffixCategory.Primary), 10), (A("dex", AffixGroup.Primary, AffixCategory.Primary), 10),
                (A("int", AffixGroup.Primary, AffixCategory.Primary), 10));
            _f.Mods.Roles["str"] = PrimaryRole.Core; _f.Mods.Roles["dex"] = PrimaryRole.Secondary; _f.Mods.Roles["int"] = PrimaryRole.Tertiary;
            var d = Count(Roller(), EquipSlot.Head, "kael", 1, 28000);
            Assert.AreEqual(2 / 3.5, d["str"] / 28000.0, 0.012);
            Assert.AreEqual(1 / 3.5, d["dex"] / 28000.0, 0.012);
            Assert.AreEqual(0.5 / 3.5, d["int"] / 28000.0, 0.012);
        }

        [Test]
        public void SlotWeightsDriveTheDraw_AndZeroIsExcluded()
        {
            Pool(EquipSlot.Chest, (A("a"), 20), (A("b"), 10), (A("c"), 0));
            var d = Count(Roller(), EquipSlot.Chest, "kael", 1, 20000);
            Assert.AreEqual(2 / 3.0, d["a"] / 20000.0, 0.012);
            Assert.IsFalse(d.ContainsKey("c"));
        }

        [Test]
        public void ExhaustedPool_StopsWithoutRepeating()
        {
            Pool(EquipSlot.Head, (A("a"), 10), (A("b"), 10));
            var item = Item(Rarity.Epic); var rng = GenFixture.Rng(1);
            Roller().RollRandom(item, null, EquipSlot.Head, "kael", 4, 3, 0f, ref rng);
            Assert.AreEqual(2, item.Affixes.Count);
        }

        [Test]
        public void GroupLimits_AffinityComfortResistancePrimary()
        {
            var ember = new TagMask(Tag.Ember);
            Pool(EquipSlot.Head,
                (A("e1", AffixGroup.Affinity, affinity: ember), 10), (A("e2", AffixGroup.Affinity, affinity: ember), 10),
                (A("e3", AffixGroup.Affinity, affinity: ember), 10), (A("inf", AffixGroup.Affinity, AffixCategory.Infusion, affinity: ember), 10),
                (A("c1", AffixGroup.Comfort), 10), (A("c2", AffixGroup.Comfort), 10),
                (A("r1", AffixGroup.SingleResistance), 10), (A("r2", AffixGroup.SingleResistance), 10),
                (A("p1", AffixGroup.Primary, AffixCategory.Primary), 10), (A("p2", AffixGroup.Primary, AffixCategory.Primary), 10),
                (A("x", AffixGroup.None), 10));
            var roller = Roller(); var rng = GenFixture.Rng(31);
            for (int i = 0; i < 2000; i++)
            {
                var item = Item(Rarity.Rare);
                roller.RollRandom(item, null, EquipSlot.Head, "kael", 6, 3, 0f, ref rng);
                int aff = 0, com = 0, res = 0, pri = 0;
                foreach (var a in item.Affixes)
                {
                    if (a.AffixId[0] == 'e' || a.AffixId == "inf") aff++;
                    if (a.AffixId[0] == 'c') com++;
                    if (a.AffixId[0] == 'r') res++;
                    if (a.AffixId[0] == 'p') pri++;
                }
                Assert.LessOrEqual(aff, 2); Assert.LessOrEqual(com, 1); Assert.LessOrEqual(res, 1); Assert.LessOrEqual(pri, 1);
            }
        }

        [Test]
        public void SingleResistanceLimit_OnlyForCommonAndRare()
        {
            Pool(EquipSlot.Head, (A("r1", AffixGroup.SingleResistance), 10), (A("r2", AffixGroup.SingleResistance), 10), (A("r3", AffixGroup.SingleResistance), 10));
            var rng = GenFixture.Rng(1);
            var epic = Item(Rarity.Epic); Roller().RollRandom(epic, null, EquipSlot.Head, "kael", 3, 3, 0f, ref rng);
            Assert.AreEqual(3, epic.Affixes.Count);
            var rare = Item(Rarity.Rare); Roller().RollRandom(rare, null, EquipSlot.Head, "kael", 3, 3, 0f, ref rng);
            Assert.AreEqual(1, rare.Affixes.Count);
        }

        [Test]
        public void PrimaryLimit_AmuletTwo_NoneInRingOffHandRelic()
        {
            var p = new[] { A("p1", AffixGroup.Primary, AffixCategory.Primary), A("p2", AffixGroup.Primary, AffixCategory.Primary), A("p3", AffixGroup.Primary, AffixCategory.Primary) };
            Pool(EquipSlot.Amulet, (p[0], 10), (p[1], 10), (p[2], 10));
            var rng = GenFixture.Rng(1);
            var item = Item(Rarity.Epic); Roller().RollRandom(item, null, EquipSlot.Amulet, "kael", 3, 3, 0f, ref rng);
            Assert.AreEqual(2, item.Affixes.Count);
            foreach (var slot in new[] { EquipSlot.Ring, EquipSlot.OffHand, EquipSlot.Relic })
            {
                Pool(slot, (p[0], 10), (p[1], 10));
                var other = Item(Rarity.Epic); Roller().RollRandom(other, null, slot, "kael", 2, 3, 0f, ref rng);
                Assert.AreEqual(0, other.Affixes.Count, slot.ToString());
            }
        }

        [Test]
        public void Restrictions_WeaponTag_Warbrand_Orun_RelicFamily()
        {
            var sword = _f.Items.GetBase("kael_sword"); var staff = _f.Items.GetBase("lyra_staff");
            var roller = Roller();
            var mh = Count(roller, EquipSlot.MainHand, "kael", 3, 3000, Rarity.Epic, sword);
            Assert.IsTrue(mh.ContainsKey("atk_speed")); Assert.IsFalse(mh.ContainsKey("cast_speed"));
            var st = Count(roller, EquipSlot.MainHand, "lyra", 3, 3000, Rarity.Epic, staff);
            Assert.IsTrue(st.ContainsKey("cast_speed")); Assert.IsFalse(st.ContainsKey("atk_speed"));

            var warbrand = _f.Items.GetBase("kael_warbrand"); var grimoire = _f.Items.GetBase("lyra_grimoire");
            Assert.IsTrue(Count(roller, EquipSlot.OffHand, "kael", 3, 3000, Rarity.Epic, warbrand).ContainsKey("block"));
            Assert.IsFalse(Count(roller, EquipSlot.OffHand, "lyra", 3, 3000, Rarity.Epic, grimoire).ContainsKey("block"));

            Assert.IsFalse(Count(roller, EquipSlot.MainHand, "kael", 3, 3000, Rarity.Epic, sword).ContainsKey("summon_dmg"));
            Assert.IsTrue(Count(roller, EquipSlot.MainHand, "orun", 3, 3000, Rarity.Epic, _f.Items.GetBase("orun_scepter")).ContainsKey("summon_dmg"));
            Assert.IsTrue(Count(roller, EquipSlot.Ring, "kael", 3, 3000, Rarity.Epic, _f.Items.GetBase("band")).ContainsKey("summon_dmg")); // Ring/Relic: any hero

            var relic = Count(roller, EquipSlot.Relic, "kael", 3, 3000, Rarity.Epic, _f.Items.GetBase("relic_cinder"));
            Assert.IsTrue(relic.ContainsKey("ember") && relic.ContainsKey("inf_ember"));
            Assert.IsFalse(relic.ContainsKey("frost") || relic.ContainsKey("storm"));
        }

        [Test]
        public void MovementSpeed_OnlyFeet()
        {
            var roller = Roller();
            Assert.IsTrue(Count(roller, EquipSlot.Feet, "kael", 4, 2000, Rarity.Epic).ContainsKey("move_speed"));
            foreach (EquipSlot s in System.Enum.GetValues(typeof(EquipSlot)))
                if (s != EquipSlot.Feet) Assert.IsFalse(Count(roller, s, "kael", 4, 500, Rarity.Epic).ContainsKey("move_speed"), s.ToString());
        }

        [Test]
        public void UnknownRestriction_Throws()
        {
            var a = A("a");
            Pool(EquipSlot.Head, (a, 10));
            _f.Items.PoolList[0].Restriction = "bogus:code";
            Assert.Throws<System.InvalidOperationException>(() => { var r = GenFixture.Rng(1); Roller().RollRandom(Item(Rarity.Rare), null, EquipSlot.Head, "kael", 1, 3, 0f, ref r); });
        }
    }

    [TestFixture]
    public class QualityRollTests
    {
        private GenFixture _f;
        [SetUp] public void SetUp() => _f = GenFixture.Create();

        [Test]
        public void ResampledFloor_NoMassAtTheFloor()
        {
            var rng = GenFixture.Rng(8); double sum = 0; int atFloor = 0; const int n = 20000;
            for (int i = 0; i < n; i++)
            {
                float r = Sampling.RollQuality(ref rng, 0.2f);
                Assert.GreaterOrEqual(r, 0.2f); Assert.Less(r, 1f);
                if (r == 0.2f) atFloor++;
                sum += r;
            }
            Assert.AreEqual(0.6, sum / n, 0.01); // resampled U(0.2, 1)
            Assert.Less(atFloor, 3);             // a clamp would put ~20% here
        }

        private List<ItemInstance> Gen(LootContext c, int n, ulong seed = 21)
        {
            var g = _f.NewGenerator(); var rng = GenFixture.Rng(seed); var l = new List<ItemInstance>();
            for (int i = 0; i < n; i++) l.Add(g.Generate(c, ref rng));
            return l;
        }

        [TestCase(Difficulty.Normal, 0, 0.0)] [TestCase(Difficulty.Veteran, 0, 0.0)]
        [TestCase(Difficulty.Nightmare, 0, 0.10)] [TestCase(Difficulty.Eclipse, 0, 0.20)] [TestCase(Difficulty.Ascension, 100, 0.65)]
        public void DifficultyFloor_AppliesToAffixesImplicitAndModifiers(Difficulty d, int asc, double floor)
        {
            var c = GenFixture.Ctx(LootSourceKind.Boss, d, "ashen_wastes", "kael", 60, asc);
            c.Guarantees.ForceEpicPlus = true;
            var all = new List<float>();
            foreach (var it in Gen(c, 600))
            {
                if (it.Flags.HasFlag(ItemFlags.Primal)) continue;
                foreach (var a in it.Affixes) all.Add(a.Roll01);
                foreach (var m in it.Modifiers) all.Add(m.Roll01);
            }
            Assert.Greater(all.Count, 1000);
            foreach (var r in all) Assert.GreaterOrEqual(r, (float)floor - 1e-6f);
            double mean = 0; foreach (var r in all) mean += r; mean /= all.Count;
            Assert.AreEqual((1 + floor) / 2, mean, 0.02);
        }

        [Test]
        public void Primal_OnlyAscension50Plus_OnlyLegendary_RollsAbove085()
        {
            var c = GenFixture.Ctx(LootSourceKind.Boss, Difficulty.Ascension, null, "kael", 100, 100);
            c.Guarantees.ForceLegendary = true; c.Guarantees.ForceKind = ItemKind.NamedLegendary;
            var items = Gen(c, 3000);
            int primal = 0;
            foreach (var it in items)
            {
                Assert.AreEqual(Rarity.Legendary, it.Rarity);
                if (!it.Flags.HasFlag(ItemFlags.Primal)) continue;
                primal++;
                foreach (var a in it.Affixes) Assert.GreaterOrEqual(a.Roll01, 0.85f);
                foreach (var m in it.Modifiers) Assert.GreaterOrEqual(m.Roll01, 0.85f);
                if (it.ImplicitRoll01 != 0f) Assert.GreaterOrEqual(it.ImplicitRoll01, 0.85f); // 0 = base without implicit
            }
            Assert.AreEqual(0.10, primal / 3000.0, 0.02); // 2% + 0.16% x 50 = 10% at A100

            c.AscensionLevel = 49;
            foreach (var it in Gen(c, 500)) Assert.IsFalse(it.Flags.HasFlag(ItemFlags.Primal));
            var ec = GenFixture.Ctx(LootSourceKind.Boss, Difficulty.Eclipse, null, "kael", 80);
            ec.Guarantees.ForceLegendary = true;
            foreach (var it in Gen(ec, 500)) Assert.IsFalse(it.Flags.HasFlag(ItemFlags.Primal));
            var generic = GenFixture.Ctx(LootSourceKind.Trash, Difficulty.Ascension, null, "kael", 100, 100);
            foreach (var it in Gen(generic, 500)) if (it.Rarity != Rarity.Legendary) Assert.IsFalse(it.Flags.HasFlag(ItemFlags.Primal));
        }
    }

    [TestFixture]
    public class EpicModifierTests
    {
        private GenFixture _f;
        [SetUp] public void SetUp() => _f = GenFixture.Create();

        private ItemInstance One(EpicModifierRoller r, LootContext c, string hero, ref Xoshiro128StarStar rng)
        {
            var item = new ItemInstance { Rarity = Rarity.Epic };
            r.Roll(item, hero, EquipSlot.Head, c, 1, 0f, ref rng);
            return item;
        }

        [Test]
        public void HeroBranch75_BindsHero_UniversalBranchDoesNot()
        {
            var r = new EpicModifierRoller(_f.Loot, _f.Mods); var rng = GenFixture.Rng(5); var c = GenFixture.Ctx();
            int bound = 0; const int n = 20000;
            for (int i = 0; i < n; i++)
            {
                var it = One(r, c, "kael", ref rng);
                Assert.AreEqual(1, it.Modifiers.Count);
                bool gen = it.Modifiers[0].ModifierId.StartsWith("GEN-");
                Assert.AreEqual(!gen, it.BoundHeroId != null);
                if (!gen) { Assert.AreEqual("kael", it.BoundHeroId); bound++; }
            }
            Assert.AreEqual(0.75, bound / (double)n, 0.01);
        }

        [Test]
        public void EquippedSkill_WeightX3()
        {
            var r = new EpicModifierRoller(_f.Loot, _f.Mods); var rng = GenFixture.Rng(5);
            var c = GenFixture.Ctx(); c.EquippedSkillIds = new List<string> { "skill.cleave" };
            int cleave = 0, heroBranch = 0;
            for (int i = 0; i < 30000; i++)
            {
                var it = One(r, c, "kael", ref rng);
                if (it.BoundHeroId == null) continue;
                heroBranch++; if (it.Modifiers[0].ModifierId == "KAE-CS-01") cleave++;
            }
            Assert.AreEqual(0.75, cleave / (double)heroBranch, 0.012); // 3 : 1
        }

        [Test]
        public void EmptyHeroPool_FallsBackToUniversalWithoutBinding()
        {
            _f.Mods.HeroMods["kael"].Clear();
            var r = new EpicModifierRoller(_f.Loot, _f.Mods); var rng = GenFixture.Rng(5);
            for (int i = 0; i < 200; i++)
            {
                var it = One(r, GenFixture.Ctx(), "kael", ref rng);
                StringAssert.StartsWith("GEN-", it.Modifiers[0].ModifierId); Assert.IsNull(it.BoundHeroId);
            }
        }

        [Test]
        public void GeneratedEpic_HasOneModifierFromHeroOrUniversal()
        {
            var g = _f.NewGenerator(); var rng = GenFixture.Rng(5);
            var c = GenFixture.Ctx(LootSourceKind.Reliquary, ilvl: 50); int epics = 0;
            for (int i = 0; i < 400; i++)
            {
                var it = g.Generate(c, ref rng);
                if (it.Rarity != Rarity.Epic) continue;
                epics++;
                Assert.AreEqual(1, it.Modifiers.Count);
                Assert.AreEqual(it.Modifiers[0].ModifierId.StartsWith("GEN-"), it.BoundHeroId == null);
            }
            Assert.Greater(epics, 300);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Eclipse.AI.Telegraphs;
using Eclipse.Core.Damage;
using Eclipse.Core.RunGen.Encounters;
using Eclipse.Core.Tags;
using Eclipse.Data.Definitions;
using Eclipse.Data.Difficulties;
using Eclipse.Data.Enemies;
using Eclipse.Editor.Validation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Tests.EditMode.AI.Telegraphs
{
    public class TelegraphWindupRuleTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _created) Object.DestroyImmediate(o);
            _created.Clear();
        }

        private T Make<T>(string id) where T : Definition
        {
            var d = ScriptableObject.CreateInstance<T>();
            d.name = id;
            d.EditorSetStableId(id);
            _created.Add(d);
            return d;
        }

        private AttackDefinition Attack(string id, float windup, TelegraphType type = TelegraphType.Red,
            TelegraphShape shape = TelegraphShape.Circle, AttackDelivery delivery = AttackDelivery.Melee, HitFlags flags = HitFlags.None)
        {
            var a = Make<AttackDefinition>(id);
            a.EditorSetScalars(AttackTrigger.Action, delivery, 2f, 2f, 1f, "", type, shape, 1f, 0f, 3f, 1f, 0f, windup,
                6, 30, 1f, 1f, flags, 1, TokenKind.Melee, false, 1f, 0f, "", 0f);
            return a;
        }

        private DifficultyDefinition Difficulty(string id, float reduction)
        {
            var d = Make<DifficultyDefinition>(id);
            d.EditorSet(DifficultyTier.Normal, 3, 2, 1.2f, 1, reduction, 1f, 1f, 1f);
            return d;
        }

        private EnemyDefinition Enemy(string id, EnemyLootClass loot, params AttackDefinition[] attacks)
        {
            var e = Make<EnemyDefinition>(id);
            e.EditorSetScalars(EnemyArchetype.Standard, "Hollow", 1f, 1f, 3, EnemyMassClass.Medium, 40f, 4f, DamageType.Physical,
                DamageType.Physical, new float[7], 0f, loot, 0UL, 0f, 2f, RepositionBehavior.Approach, 0f, 0f, 0f, 0f, "hfsm.melee");
            e.EditorSetAttacks(new List<AttackDefinition>(attacks));
            return e;
        }

        private static int Errors(IEnumerable<Definition> defs)
        {
            var ctx = new ValidationContext(defs.ToList(), new Dictionary<string, string>());
            new TelegraphWindupRule().Validate(ctx);
            return ctx.Report.Messages.Count(m => m.Rule == "TelegraphWindup" && m.Severity == ValidationSeverity.Error);
        }

        [SetUp]
        public void NoPalette() => TelegraphWindupRule.PaletteProvider = () => null;

        [OneTimeTearDown]
        public void RestorePalette()
        {
            TelegraphWindupRule.PaletteProvider = () => AssetDatabase.LoadAssetAtPath<TelegraphPalette>(Eclipse.Editor.Setup.TelegraphPaletteCreator.AssetPath);
        }

        [Test]
        public void IsDiscovered() =>
            CollectionAssert.Contains(DataValidator.DiscoverRules().Select(r => r.Name).ToList(), "TelegraphWindup");

        [Test]
        public void Passes_CommonAt0_6WithAll4Difficulties()
        {
            var defs = new List<Definition>
            {
                Difficulty("difficulty.normal", 0.15f), Difficulty("difficulty.eclipse", 0.15f),
                Attack("attack.t.ok", 0.6f),
            };
            Assert.AreEqual(0, Errors(defs));
        }

        [Test]
        public void Fails_WhenReducedWindupFallsBelowFloorInAnyDifficulty()
        {
            // 0.5 s: ok with 0% reduction (>= 0.45) but 0.425 s with 15% -> below the common floor.
            var defs = new List<Definition>
            {
                Difficulty("difficulty.normal", 0f), Difficulty("difficulty.eclipse", 0.15f),
                Attack("attack.t.fast", 0.5f),
            };
            Assert.AreEqual(1, Errors(defs));
        }

        [Test]
        public void Fails_EliteAttackUnderEliteFloor_ButCommonPasses()
        {
            AttackDefinition a = Attack("attack.t.elite", 0.6f); // 0.51 reduced < 0.55
            var defs = new List<Definition> { Difficulty("difficulty.eclipse", 0.15f), a, Enemy("enemy.t.elite", EnemyLootClass.Elite, a) };
            Assert.AreEqual(1, Errors(defs));

            AttackDefinition c = Attack("attack.t.common", 0.6f);
            Assert.AreEqual(0, Errors(new List<Definition> { Difficulty("difficulty.eclipse", 0.15f), c, Enemy("enemy.t.common", EnemyLootClass.Standard, c) }));
        }

        [Test]
        public void Fails_AnnihilationUnder1_5_AndSniperUnder0_8()
        {
            var diff = Difficulty("difficulty.normal", 0f);
            Assert.AreEqual(1, Errors(new List<Definition> { diff, Attack("attack.t.annih", 1.2f, TelegraphType.Annihilation) }));
            Assert.AreEqual(1, Errors(new List<Definition> { diff, Attack("attack.t.annih2", 1.2f, flags: HitFlags.Annihilation) }));
            Assert.AreEqual(0, Errors(new List<Definition> { diff, Attack("attack.t.annih3", 1.5f, TelegraphType.Annihilation) }));
            Assert.AreEqual(1, Errors(new List<Definition> { diff, Attack("attack.t.sniper", 0.6f, shape: TelegraphShape.Line, delivery: AttackDelivery.Projectile) }));
            Assert.AreEqual(0, Errors(new List<Definition> { diff, Attack("attack.t.sniper2", 0.8f, shape: TelegraphShape.Line, delivery: AttackDelivery.Projectile) }));
        }

        [Test]
        public void Fails_OrangePaletteEntry()
        {
            var palette = ScriptableObject.CreateInstance<TelegraphPalette>();
            _created.Add(palette);
            palette.EditorSetEntries(new[]
            {
                new TelegraphPalette.Entry { visual = TelegraphVisual.Red, fill = new Color(1f, 0.5f, 0f), border = Color.red },
            });
            TelegraphWindupRule.PaletteProvider = () => palette;
            Assert.AreEqual(1, Errors(new List<Definition> { Difficulty("difficulty.normal", 0f), Attack("attack.t.col", 0.6f) }));
        }

        [Test]
        public void CurrentProjectData_PassesValidation()
        {
            TelegraphWindupRule.PaletteProvider = () => AssetDatabase.LoadAssetAtPath<TelegraphPalette>(Eclipse.Editor.Setup.TelegraphPaletteCreator.AssetPath);
            var defs = new List<Definition>();
            foreach (string guid in AssetDatabase.FindAssets("t:Definition", new[] { "Assets/_Project/Content" }))
            {
                var d = AssetDatabase.LoadAssetAtPath<Definition>(AssetDatabase.GUIDToAssetPath(guid));
                if (d is AttackDefinition || d is EnemyDefinition || d is DifficultyDefinition) defs.Add(d);
            }
            Assert.IsTrue(defs.OfType<AttackDefinition>().Any(), "no attacks imported");
            Assert.AreEqual(0, Errors(defs));
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Eclipse.Data.Definitions;
using Eclipse.Editor.Validation;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Data.Validation.Tests
{
    public class RuleTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _created) Object.DestroyImmediate(o);
            _created.Clear();
        }

        private T Make<T>(string id, string nameEn = null, string sourceDoc = null, bool displayName = true)
            where T : Definition
        {
            T d = FakeValidationDef.Make<T>(id, nameEn, sourceDoc, displayName);
            _created.Add(d);
            return d;
        }

        private static int Count(ValidationReport r, string rule, ValidationSeverity sev) =>
            r.Messages.Count(m => m.Rule == rule && m.Severity == sev);

        private static readonly Dictionary<string, string> Reserved =
            new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "Frostbound", "07" },
                { "Poise", "-" },
            };

        [Test]
        public void DiscoversAllBuiltInRules()
        {
            var names = DataValidator.DiscoverRules().Select(r => r.Name).ToList();
            foreach (string n in new[] { "UniqueId", "IdFormat", "TagVocabulary", "LocalizationKey", "ReservedName", "ReferenceResolves" })
                CollectionAssert.Contains(names, n);
        }

        [Test]
        public void UniqueId_Passes_And_Fails()
        {
            var ok = DataValidator.Validate(new Definition[] { Make<FakeValidationDef>("a.one"), Make<FakeValidationDef>("a.two") });
            Assert.AreEqual(0, Count(ok, "UniqueId", ValidationSeverity.Error));
            var bad = DataValidator.Validate(new Definition[] { Make<FakeValidationDef>("a.one"), Make<FakeValidationDef>("a.one") });
            Assert.AreEqual(1, Count(bad, "UniqueId", ValidationSeverity.Error));
        }

        [Test]
        public void IdFormat_Passes_And_Fails()
        {
            var ok = DataValidator.Validate(new Definition[] { Make<FakeValidationDef>("hero.kael") });
            Assert.AreEqual(0, Count(ok, "IdFormat", ValidationSeverity.Error));
            var bad = DataValidator.Validate(new Definition[] { Make<FakeValidationDef>("Bad Id"), Make<FakeValidationDef>("") });
            Assert.AreEqual(2, Count(bad, "IdFormat", ValidationSeverity.Error));
        }

        [Test]
        public void TagVocabulary_Passes_And_Fails()
        {
            var good = Make<FakeTaggedDef>("t.good");
            good.Tags.AddRange(new[] { "Attack", "Ember" });
            Assert.AreEqual(0, Count(DataValidator.Validate(new Definition[] { good }), "TagVocabulary", ValidationSeverity.Error));

            var unknown = Make<FakeTaggedDef>("t.unknown");
            unknown.Tags.Add("Banana");
            Assert.AreEqual(1, Count(DataValidator.Validate(new Definition[] { unknown }), "TagVocabulary", ValidationSeverity.Error));

            var flag = Make<FakeTaggedDef>("t.flag");
            flag.Tags.Add("Unblockable");
            ValidationReport r = DataValidator.Validate(new Definition[] { flag });
            ValidationMessage m = r.Messages.Single(x => x.Rule == "TagVocabulary");
            StringAssert.Contains("R02", m.Text);
            StringAssert.Contains("HitFlag", m.Text);
        }

        [Test]
        public void LocalizationKey_Errors_When_Missing()
        {
            var ok = DataValidator.Validate(new Definition[] { Make<FakeValidationDef>("l.ok") });
            Assert.AreEqual(0, Count(ok, "LocalizationKey", ValidationSeverity.Error));
            var bad = DataValidator.Validate(new Definition[] { Make<FakeValidationDef>("l.bad", displayName: false) });
            Assert.AreEqual(1, Count(bad, "LocalizationKey", ValidationSeverity.Error));
            Assert.AreEqual(0, Count(bad, "LocalizationKey", ValidationSeverity.Warning));
        }

        [Test]
        public void ReservedName_Passes_And_Fails()
        {
            var owner = Make<FakeValidationDef>("r.owner", "Frostbound", "07-resonance");
            var other = Make<FakeValidationDef>("r.other", "frostbound", "03-modificadores");
            var banned = Make<FakeValidationDef>("r.banned", "Poise", "06-combate");
            var plain = Make<FakeValidationDef>("r.plain", "Sunfire", "03-modificadores");
            ValidationReport r = DataValidator.Validate(new Definition[] { owner, other, banned, plain }, Reserved);
            Assert.AreEqual(2, Count(r, "ReservedName", ValidationSeverity.Error));
            CollectionAssert.AreEquivalent(new[] { "r.other", "r.banned" },
                r.Messages.Where(m => m.Rule == "ReservedName").Select(m => m.Id).ToArray());
        }

        [Test]
        public void ReservedNamesCsv_LoadsFromDataSource()
        {
            var map = DataValidator.LoadReservedNames(DataValidator.DefaultReservedNamesPath());
            Assert.AreEqual("07", map["Frostbound"]);
            Assert.AreEqual("-", map["poise"]);
        }

        [Test]
        public void ReferenceResolves_Passes_And_Fails()
        {
            var target = Make<FakeValidationDef>("ref.target");
            var good = Make<FakeReferencingDef>("ref.good");
            good.Target = "ref.target";
            var bad = Make<FakeReferencingDef>("ref.bad");
            bad.Target = "ref.missing";
            ValidationReport r = DataValidator.Validate(new Definition[] { target, good, bad });
            Assert.AreEqual(1, Count(r, "ReferenceResolves", ValidationSeverity.Error));
            Assert.AreEqual("ref.bad", r.Messages.Single(m => m.Rule == "ReferenceResolves").Id);
        }
    }
}

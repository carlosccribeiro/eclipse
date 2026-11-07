using Eclipse.Core.Tags;
using NUnit.Framework;

namespace Eclipse.Core.Stats.Tests
{
    public class StatBlockBucketTests
    {
        [Test]
        public void SumBucket_And_ProductMore_FilterByTags()
        {
            var sb = new StatBlock(new StatRuleTable());
            var src = sb.CreateSource();
            sb.AddMod(src, new StatMod(StatId.PhysicalDamage, ModBucket.Increased, 0.30f));
            sb.AddMod(src, new StatMod(StatId.PhysicalDamage, ModBucket.Increased, 0.15f, requiredTags: new TagMask(Tag.Area)));
            sb.AddMod(src, new StatMod(StatId.PhysicalDamage, ModBucket.Increased, 1.00f, excludedTags: new TagMask(Tag.Area)));
            sb.AddMod(src, new StatMod(StatId.PhysicalDamage, ModBucket.Flat, 5f));
            sb.AddMod(src, new StatMod(StatId.PhysicalDamage, ModBucket.More, 0.10f));
            sb.AddMod(src, new StatMod(StatId.PhysicalDamage, ModBucket.More, 0.20f, requiredTags: new TagMask(Tag.Area)));

            var area = new TagMask(Tag.Area);
            Assert.That(sb.SumBucket(StatId.PhysicalDamage, ModBucket.Increased, area, null), Is.EqualTo(0.45f).Within(1e-5f));
            Assert.That(sb.SumBucket(StatId.PhysicalDamage, ModBucket.Increased, TagMask.None, null), Is.EqualTo(1.30f).Within(1e-5f));
            Assert.That(sb.SumBucket(StatId.PhysicalDamage, ModBucket.Flat, TagMask.None, null), Is.EqualTo(5f));
            Assert.That(sb.ProductMore(StatId.PhysicalDamage, area, null), Is.EqualTo(1.32f).Within(1e-5f));
            Assert.That(sb.ProductMore(StatId.PhysicalDamage, TagMask.None, null), Is.EqualTo(1.10f).Within(1e-5f));
        }
    }
}

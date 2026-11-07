using Eclipse.Core.Stats;
using Eclipse.Core.Tags;

namespace Eclipse.Core.Damage
{
    /// <summary>Damage types (06 3.1): Physical plus the six affinities. Values index fixed-size arrays; never reorder.</summary>
    public enum DamageType : byte
    {
        Physical = 0,
        Ember = 1,
        Frost = 2,
        Storm = 3,
        Void = 4,
        Echo = 5,
        Ruin = 6,
    }

    /// <summary>Mitigation family of a damage type (06 3.1).</summary>
    public enum MitigationFamily : byte
    {
        Physical,
        Elemental,
        /// <summary>"Primordial" group: Void, Echo and Ruin are all mitigated by Void Resistance.</summary>
        Void,
    }

    /// <summary>Helpers for <see cref="DamageType"/>.</summary>
    public static class DamageTypes
    {
        public const int Count = 7;

        /// <summary>Mask of the seven element tags (Physical, Ember..Ruin): they describe a component, not a source.</summary>
        public static readonly TagMask AllTypeTags = new TagMask(Tag.Physical)
            .With(Tag.Ember).With(Tag.Frost).With(Tag.Storm).With(Tag.Void).With(Tag.Echo).With(Tag.Ruin);

        public static MitigationFamily Family(this DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical: return MitigationFamily.Physical;
                case DamageType.Ember:
                case DamageType.Frost:
                case DamageType.Storm: return MitigationFamily.Elemental;
                default: return MitigationFamily.Void;
            }
        }

        /// <summary>Vocabulary tag matching the type (00 3.4).</summary>
        public static Tag ToTag(this DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical: return Tag.Physical;
                case DamageType.Ember: return Tag.Ember;
                case DamageType.Frost: return Tag.Frost;
                case DamageType.Storm: return Tag.Storm;
                case DamageType.Void: return Tag.Void;
                case DamageType.Echo: return Tag.Echo;
                default: return Tag.Ruin;
            }
        }

        /// <summary>The "[Type] Damage" Increased stat (01 3.5).</summary>
        public static StatId ToDamageStat(this DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical: return StatId.PhysicalDamage;
                case DamageType.Ember: return StatId.EmberDamage;
                case DamageType.Frost: return StatId.FrostDamage;
                case DamageType.Storm: return StatId.StormDamage;
                case DamageType.Void: return StatId.VoidDamage;
                case DamageType.Echo: return StatId.EchoDamage;
                default: return StatId.RuinDamage;
            }
        }
    }
}

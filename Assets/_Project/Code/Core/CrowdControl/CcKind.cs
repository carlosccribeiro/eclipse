namespace Eclipse.Core.CrowdControl
{
    /// <summary>Crowd-control state subject to diminishing returns (06 §3.8.3).</summary>
    public enum CcKind
    {
        Stun = 0,
        Freeze = 1,
        Root = 2,
        Silence = 3,
        Taunt = 4,
    }

    /// <summary>Mapping helpers for <see cref="CcKind"/>.</summary>
    public static class CcKindExtensions
    {
        /// <summary>DR group of a kind: Stun and Freeze are Hard; the others map to their own group.</summary>
        public static CcGroup Group(this CcKind kind)
        {
            switch (kind)
            {
                case CcKind.Stun:
                case CcKind.Freeze: return CcGroup.Hard;
                case CcKind.Root: return CcGroup.Root;
                case CcKind.Silence: return CcGroup.Silence;
                case CcKind.Taunt: return CcGroup.Taunt;
                default: throw new System.ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}

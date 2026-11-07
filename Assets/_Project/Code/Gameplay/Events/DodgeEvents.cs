using UnityEngine;

namespace Eclipse.Gameplay.Events
{
    /// <summary>Published when a dodge starts (06 3.7). Publish through EventBus&lt;OnDodge&gt;.</summary>
    public readonly struct OnDodge
    {
        public readonly int Frame;
        public readonly Vector3 Origin;
        public readonly Vector3 Direction;
        public readonly int ChargesLeft;

        public OnDodge(int frame, Vector3 origin, Vector3 direction, int chargesLeft)
        {
            Frame = frame; Origin = origin; Direction = direction; ChargesLeft = chargesLeft;
        }
    }

    /// <summary>Published when a hit is negated inside the Perfect Dodge window (06 3.7; hero resource per 08).</summary>
    public readonly struct OnPerfectDodge
    {
        public readonly int ImpactFrame;

        public OnPerfectDodge(int impactFrame) { ImpactFrame = impactFrame; }
    }
}

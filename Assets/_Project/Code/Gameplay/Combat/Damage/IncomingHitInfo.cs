namespace Eclipse.Gameplay.Combat
{
    /// <summary>Per-hit context that <see cref="IDamageReceiver.ReceiveHit"/> cannot carry (frozen contract): attacker eLvl and telegraph.</summary>
    public readonly struct IncomingHitInfo
    {
        /// <summary>Monster Level of the attacker (K of Armor, 01 3.4.1).</summary>
        public readonly float AttackerLevel;
        /// <summary>Attack had a telegraph; Dodge Chance only rolls when false (06 3.3 step 2).</summary>
        public readonly bool IsTelegraphed;

        public IncomingHitInfo(float attackerLevel, bool isTelegraphed) { AttackerLevel = attackerLevel; IsTelegraphed = isTelegraphed; }
    }
}

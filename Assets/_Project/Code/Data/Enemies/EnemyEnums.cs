namespace Eclipse.Data.Enemies
{
    /// <summary>Mass class of an enemy (06 section 3.8.1).</summary>
    public enum EnemyMassClass : byte { Light, Medium, Heavy, Colossal }

    /// <summary>Loot classes handed to 05-loot (09 section 10.5).</summary>
    public enum EnemyLootClass : byte { Fodder, Standard, Heavy, Specialist, Elite, Guardian, Boss }

    /// <summary>Repositioning behavior (09 section 3.1).</summary>
    public enum RepositionBehavior : byte { Approach, KeepDistance, Flank, Retreat }

    /// <summary>
    /// Telegraph kinds of 06 section 3.10. There is deliberately no orange/yellow entry: that color is reserved to
    /// Ember VFX and is never a telegraph. CyanRing is the interruption window ring around the enemy.
    /// </summary>
    public enum TelegraphType : byte { Red, Purple, WhiteGold, CyanRing, Annihilation, UnstoppableOutline }

    /// <summary>Telegraph ground shapes (09 section 3.3).</summary>
    public enum TelegraphShape : byte { Circle, Cone, Line, Ring }

    /// <summary>Which attack token pool an attack consumes (09 section 3.2).</summary>
    public enum TokenKind : byte { None, Melee, Ranged }

    /// <summary>When an attack is used: chosen by the utility selector, or triggered by the enemy dying.</summary>
    public enum AttackTrigger : byte { Action, OnDeath }

    /// <summary>How the attack reaches the target (09 section 4).</summary>
    public enum AttackDelivery : byte { Melee, Projectile, Lobbed, OnDeath }
}

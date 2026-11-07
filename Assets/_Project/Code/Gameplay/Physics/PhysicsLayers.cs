namespace Eclipse.Gameplay.Physics
{
    /// <summary>
    /// Physics layer indices and explicit query masks (11 section 3.9; 06 section 3.9).
    /// Indices must match ProjectSettings/TagManager.asset; the collision matrix lives in DynamicsManager.asset.
    /// Hitboxes and hurtboxes are query shapes only: they never collide physically.
    /// Only Character collides physically (with Default, Character, Environment, Wall, LowCover).
    /// </summary>
    public static class PhysicsLayers
    {
        public const int Character = 8;
        public const int Environment = 9;
        public const int Wall = 10;
        public const int LowCover = 11;
        public const int HurtboxPlayer = 12;
        public const int HurtboxEnemy = 13;
        public const int HitboxPlayer = 14;
        public const int HitboxEnemy = 15;
        public const int Projectile = 16;
        public const int Interactable = 17;

        /// <summary>Layer names as registered in the TagManager, in index order starting at <see cref="Character"/>.</summary>
        public static readonly string[] Names =
        {
            "Character", "Environment", "Wall", "LowCover", "Hurtbox_Player",
            "Hurtbox_Enemy", "Hitbox_Player", "Hitbox_Enemy", "Projectile", "Interactable",
        };

        public const int CharacterMask = 1 << Character;
        public const int EnvironmentMask = 1 << Environment;
        public const int WallMask = 1 << Wall;
        public const int LowCoverMask = 1 << LowCover;
        public const int HurtboxPlayerMask = 1 << HurtboxPlayer;
        public const int HurtboxEnemyMask = 1 << HurtboxEnemy;
        public const int HitboxPlayerMask = 1 << HitboxPlayer;
        public const int HitboxEnemyMask = 1 << HitboxEnemy;
        public const int ProjectileMask = 1 << Projectile;
        public const int InteractableMask = 1 << Interactable;

        /// <summary>Movement blockers for characters.</summary>
        public const int MovementBlockersMask = EnvironmentMask | WallMask | LowCoverMask | CharacterMask;

        /// <summary>Melee line-of-sight and area LOS raycasts: only Wall blocks (06 section 3.9).</summary>
        public const int LineOfSightMask = WallMask;

        /// <summary>Projectile blockers by default: Wall (and Environment).</summary>
        public const int ProjectileBlockersMask = WallMask | EnvironmentMask;

        /// <summary>Projectile blockers for skills without the Area tag: also stopped by LowCover.</summary>
        public const int ProjectileBlockersNonAreaMask = ProjectileBlockersMask | LowCoverMask;

        /// <summary>Targets hit by player attacks.</summary>
        public const int PlayerAttackTargetsMask = HurtboxEnemyMask;

        /// <summary>Targets hit by enemy attacks.</summary>
        public const int EnemyAttackTargetsMask = HurtboxPlayerMask;
    }
}

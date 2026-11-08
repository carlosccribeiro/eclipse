using Eclipse.Core.CrowdControl;
using Eclipse.Core.Tags;
using Eclipse.Gameplay.Entities;
using Eclipse.Gameplay.Player;
using Eclipse.Gameplay.Simulation;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>Result of a CC request (06 section 3.8.3).</summary>
    public enum CcOutcome
    {
        Applied = 0,
        /// <summary>Unstoppable blocks all CC.</summary>
        BlockedUnstoppable = 1,
        /// <summary>Diminishing returns exhausted, or the player Hard immunity window.</summary>
        ImmuneDr = 2,
        /// <summary>Colossal is immune; the CC was converted into Stagger (see <see cref="CcResult.ConvertedStagger"/>).</summary>
        ConvertedToStagger = 3,
        /// <summary>Immune by role (Colossal Silence/Taunt, Guardian/boss Taunt).</summary>
        ImmuneRole = 4,
    }

    public readonly struct CcResult
    {
        public readonly CcOutcome Outcome;
        public readonly int DurationFrames;
        public readonly float ConvertedStagger;
        /// <summary>Freeze on a Colossal: the M11 Frostbound status must be applied (06 3.8.2 R10).</summary>
        public readonly bool FrostboundHook;

        public CcResult(CcOutcome outcome, int durationFrames, float convertedStagger = 0f, bool frostboundHook = false)
        { Outcome = outcome; DurationFrames = durationFrames; ConvertedStagger = convertedStagger; FrostboundHook = frostboundHook; }

        public bool Applied => Outcome == CcOutcome.Applied;
    }

    /// <summary>Displacement outcome (Knockback/Pull) after mass scaling (06 section 3.8.1).</summary>
    public readonly struct DisplacementResult
    {
        /// <summary>Distance/force actually applied (0 when ignored).</summary>
        public readonly float Amount;
        public readonly float ConvertedStagger;
        /// <summary>Pull on a Colossal: the M11 Distortion status must be applied (06 3.8.2 R10).</summary>
        public readonly bool DistortionHook;

        public DisplacementResult(float amount, float convertedStagger, bool distortionHook)
        { Amount = amount; ConvertedStagger = convertedStagger; DistortionHook = distortionHook; }
    }

    public enum PlayerHitWeight { Light = 0, Medium = 1, Heavy = 2 }

    public enum PlayerReaction { None = 0, Flash = 1, Flinch = 2, Knockdown = 3 }

    /// <summary>
    /// Generic combat states of one entity (06 section 3.8.3): hard CC with DR (Core <see cref="CrowdControlTracker"/>), Slow,
    /// Super Armor, Unstoppable, Stealth, Parry window, Intangible, Knockback/Pull scaling, Wall Impact and the player's hit
    /// reaction. Pure C#; time is passed as the simulation frame. Allocation-free after construction.
    /// Gaps/choices: see the M05 log (Parry does not publish OnParry here because CombatEntity already does).
    /// </summary>
    public sealed class StateComponent : IFixedTickable
    {
        private readonly CombatStateRules _rules;
        private readonly EntityId _id;
        private readonly CombatEventStream _events;
        private readonly bool _isPlayer;
        private readonly IMassBody _mass;
        private readonly bool _guardianOrBoss;
        private readonly CrowdControlTracker _tracker;

        private IStaggerable _stagger;
        private int _stunUntil, _freezeUntil, _rootUntil, _silenceUntil, _tauntUntil;
        private int _slowUntil;
        private float _slowMove, _slowAttack;
        private int _superArmorUntil, _unstoppableUntil, _intangibleUntil;
        private bool _attacking;
        private int _stealthUntil, _stealthIcdUntil;
        private int _parryStart = -1, _parryEnd = -1;
        private int _reactionStart, _reactionEnd;
        private PlayerReaction _reaction;

        public StateComponent(CombatStateRules rules, EntityId id, bool isPlayer, IMassBody mass,
            CombatEventStream events, bool guardianOrBoss = false)
        {
            _rules = rules ?? throw new System.ArgumentNullException(nameof(rules));
            _id = id;
            _isPlayer = isPlayer;
            _mass = mass;
            _events = events;
            _guardianOrBoss = guardianOrBoss;
            _tracker = new CrowdControlTracker(rules.Cc);
            CurrentFrame = 0;
        }

        public int CurrentFrame { get; private set; }
        /// <summary>Optional: Stagger sink for CC converted on Colossal targets.</summary>
        public void SetStagger(IStaggerable stagger) => _stagger = stagger;
        /// <summary>Optional: the entity's i-frame set (Intangible and Knockdown windows are added to it).</summary>
        public InvulnerabilityState Invulnerability { get; set; }

        private bool IsElite => _mass != null && _mass.IsElite;
        private MassClass Mass => _mass != null ? _mass.MassClass : MassClass.Medium;
        private bool IsColossal => !_isPlayer && _mass != null && _mass.MassClass == MassClass.Colossal;

        public void Tick(in SimulationFrame frame) => Tick(frame.Index);

        public void Tick(int frame)
        {
            CurrentFrame = frame;
            if (_stealthUntil > 0 && frame >= _stealthUntil) EndStealth(frame);
            if (_reaction != PlayerReaction.None && frame >= _reactionEnd) _reaction = PlayerReaction.None;
        }

        // ---------------------------------------------------------------- Hard CC

        public bool IsStunned(int frame) => frame < _stunUntil;
        public bool IsFrozen(int frame) => frame < _freezeUntil;
        public bool IsHardCcd(int frame) => IsStunned(frame) || IsFrozen(frame);
        public bool IsRooted(int frame) => frame < _rootUntil;
        public bool IsSilenced(int frame) => frame < _silenceUntil;
        public bool IsTaunted(int frame) => frame < _tauntUntil;
        /// <summary>Hard CC: cannot act or move.</summary>
        public bool CanAct(int frame) => !IsHardCcd(frame);
        /// <summary>Root: can attack and cast, cannot move or dodge.</summary>
        public bool CanMove(int frame) => !IsHardCcd(frame) && !IsRooted(frame);
        public bool CanDodge(int frame) => CanMove(frame);
        public bool CanCastSkills(int frame) => !IsHardCcd(frame) && !IsSilenced(frame);

        /// <summary>
        /// Requests a CC. Order: role/Unstoppable gates -> Colossal conversion -> Taunt cap -> Elite scaling ->
        /// Status Resistance (player) -> DR / player caps through <see cref="CrowdControlTracker"/>.
        /// </summary>
        public CcResult ApplyCc(CcKind kind, float baseSeconds, EntityId source, int frame, int generation, float statusResistance = 0f)
        {
            CurrentFrame = frame;
            if (kind == CcKind.Taunt && (_isPlayer || _guardianOrBoss)) return new CcResult(CcOutcome.ImmuneRole, 0);
            if (IsUnstoppable(frame)) return new CcResult(CcOutcome.BlockedUnstoppable, 0);

            if (IsColossal)
            {
                if (kind == CcKind.Stun || kind == CcKind.Root || kind == CcKind.Freeze)
                {
                    float converted = baseSeconds * _rules.V.CcToStaggerPerSecond;
                    if (_stagger != null) _stagger.AddStagger(converted, source, generation);
                    return new CcResult(CcOutcome.ConvertedToStagger, 0, converted, kind == CcKind.Freeze);
                }
                return new CcResult(CcOutcome.ImmuneRole, 0);
            }

            float seconds = baseSeconds;
            if (kind == CcKind.Taunt && seconds > _rules.V.TauntMaxSeconds) seconds = _rules.V.TauntMaxSeconds;
            if (!_isPlayer && IsElite)
                seconds = seconds * (kind == CcKind.Taunt ? _rules.V.TauntEliteMult : _rules.V.EliteCcDurationMult)
                    - CombatStateRules.FloatNoiseSeconds; // 2 x 0.6f must stay 72 frames, not 73

            int ticks = _tracker.TryApply(kind, seconds, _isPlayer, _isPlayer ? statusResistance : 0f, frame);
            if (ticks <= 0) return new CcResult(CcOutcome.ImmuneDr, 0);

            int until = frame + ticks;
            switch (kind)
            {
                case CcKind.Stun: if (until > _stunUntil) _stunUntil = until; break;
                case CcKind.Freeze: if (until > _freezeUntil) _freezeUntil = until; break;
                case CcKind.Root: if (until > _rootUntil) _rootUntil = until; break;
                case CcKind.Silence: if (until > _silenceUntil) _silenceUntil = until; break;
                default: if (until > _tauntUntil) _tauntUntil = until; break;
            }
            _events?.Publish(new OnCCApplied(frame, generation, source, _id, kind, ticks));
            return new CcResult(CcOutcome.Applied, ticks);
        }

        // ---------------------------------------------------------------- Displacement

        /// <summary>
        /// Scales a knockback by mass class (Light 100% / Medium 70% / Heavy 30% / Colossal 0%, Elite x0.6). Ignored under
        /// Super Armor/Unstoppable. On a Colossal the base CC duration converts to Stagger.
        /// </summary>
        public DisplacementResult ApplyKnockback(float force, float baseCcSeconds, EntityId source, int frame, int generation)
        {
            CurrentFrame = frame;
            if (IsUnstoppable(frame) || HasSuperArmor(frame)) return new DisplacementResult(0f, 0f, false);
            if (IsColossal)
            {
                float converted = baseCcSeconds * _rules.V.CcToStaggerPerSecond;
                if (converted > 0f && _stagger != null) _stagger.AddStagger(converted, source, generation);
                return new DisplacementResult(0f, converted, false);
            }
            float f = _isPlayer ? 1f : _rules.KnockbackFactor(Mass, IsElite);
            return new DisplacementResult(force * f, 0f, false);
        }

        /// <summary>Pull scaling by mass; on a Colossal nothing moves and the Distortion hook is raised (no DR, no Stagger).</summary>
        public DisplacementResult ApplyPull(float force, int frame)
        {
            CurrentFrame = frame;
            if (IsUnstoppable(frame)) return new DisplacementResult(0f, 0f, false);
            if (IsColossal) return new DisplacementResult(0f, 0f, true);
            float f = _isPlayer ? 1f : _rules.KnockbackFactor(Mass, IsElite);
            return new DisplacementResult(force * f, 0f, false);
        }

        /// <summary>Wall Impact: damage = 10% of the original hit, Stun 0.5 s (through DR). Returns the damage to deal.</summary>
        public float WallImpact(float originalHitDamage, EntityId source, int frame, int generation, UnityEngine.Vector3 position)
        {
            float damage = originalHitDamage * _rules.V.WallImpactDamageFraction;
            _events?.Publish(new OnWallImpact(frame, generation, source, _id, damage, position));
            ApplyCc(CcKind.Stun, _rules.V.WallImpactStunSeconds, source, frame, generation);
            return damage;
        }

        // ---------------------------------------------------------------- Slow

        /// <summary>
        /// Generic Slow: does not stack, the strongest wins (equal strength refreshes the longer duration).
        /// Fractions are clamped to the enemy cap (-60%); the player floor is applied when reading the multiplier.
        /// </summary>
        public bool ApplySlow(float moveFraction, float attackFraction, float seconds, int frame)
        {
            CurrentFrame = frame;
            if (IsUnstoppable(frame) || seconds <= 0f || moveFraction <= 0f) return false;
            float cap = _rules.V.SlowEnemyCap;
            if (moveFraction > cap) moveFraction = cap;
            if (attackFraction > cap) attackFraction = cap;
            int until = frame + CombatStateRules.Frames(seconds);
            bool active = frame < _slowUntil;
            if (active && moveFraction < _slowMove) return false;
            if (active && moveFraction == _slowMove)
            {
                if (until > _slowUntil) _slowUntil = until;
                if (attackFraction > _slowAttack) _slowAttack = attackFraction;
                return true;
            }
            _slowMove = moveFraction;
            _slowAttack = attackFraction;
            _slowUntil = until;
            return true;
        }

        /// <summary>Movement Speed multiplier from Slow: enemies 1 - slow (cap -60%); player never below 40% of base speed.</summary>
        public float MoveSpeedMultiplier(int frame)
        {
            float m = frame < _slowUntil ? 1f - _slowMove : 1f;
            return _isPlayer && m < _rules.V.SlowPlayerMinSpeed ? _rules.V.SlowPlayerMinSpeed : m;
        }

        /// <summary>Attack/Cast Speed multiplier from Slow (same cap/floor as movement).</summary>
        public float AttackSpeedMultiplier(int frame)
        {
            float m = frame < _slowUntil ? 1f - _slowAttack : 1f;
            return _isPlayer && m < _rules.V.SlowPlayerMinSpeed ? _rules.V.SlowPlayerMinSpeed : m;
        }

        // ---------------------------------------------------------------- Super Armor / Unstoppable / flinch

        public void GrantSuperArmor(int untilFrame) { if (untilFrame > _superArmorUntil) _superArmorUntil = untilFrame; }
        public void GrantUnstoppable(int untilFrame) { if (untilFrame > _unstoppableUntil) _unstoppableUntil = untilFrame; }
        /// <summary>Heavy enemies have Super Armor while attacking (06 3.8.1).</summary>
        public void SetAttacking(bool attacking) => _attacking = attacking;

        public bool IsUnstoppable(int frame) => frame < _unstoppableUntil;

        public bool HasSuperArmor(int frame) =>
            IsUnstoppable(frame) || frame < _superArmorUntil || (_attacking && !_isPlayer && Mass == MassClass.Heavy);

        /// <summary>True if a hit with this Stagger value flinches (interrupts) the target now.</summary>
        public bool ShouldFlinch(float hitStagger, int frame)
        {
            if (_isPlayer || hitStagger <= 0f || HasSuperArmor(frame)) return false;
            return hitStagger >= _rules.FlinchMinimum(Mass, IsElite);
        }

        // ---------------------------------------------------------------- Stealth

        public bool IsStealthed(int frame) => frame < _stealthUntil;
        /// <summary>False while stealthed: AI and auto-aim must skip this entity.</summary>
        public bool IsTargetable(int frame) => !IsStealthed(frame);

        /// <summary>Enters Stealth (max duration capped, ICD to re-enter counted from the previous end).</summary>
        public bool EnterStealth(float seconds, int frame)
        {
            CurrentFrame = frame;
            if (IsStealthed(frame) || frame < _stealthIcdUntil || seconds <= 0f) return false;
            if (seconds > _rules.V.StealthMaxSeconds) seconds = _rules.V.StealthMaxSeconds;
            _stealthUntil = frame + CombatStateRules.Frames(seconds);
            _events?.Publish(new OnStealthEnter(frame, 0, _id));
            return true;
        }

        /// <summary>The entity dealt damage; breaks Stealth unless the effect says otherwise.</summary>
        public void NotifyDamageDealt(int frame, bool exempt = false) { if (!exempt) BreakStealth(frame); }

        /// <summary>The entity took direct damage; breaks Stealth.</summary>
        public void NotifyDamageTaken(int frame) => BreakStealth(frame);

        /// <summary>Revealed by AoE damage or proximity (<= StealthRevealMeters from the player).</summary>
        public void Reveal(int frame) => BreakStealth(frame);

        public bool RevealIfNear(float distanceToPlayerMeters, int frame)
        {
            if (!IsStealthed(frame) || distanceToPlayerMeters > _rules.V.StealthRevealMeters) return false;
            BreakStealth(frame);
            return true;
        }

        private void BreakStealth(int frame) { if (IsStealthed(frame)) EndStealth(frame); }

        private void EndStealth(int frame)
        {
            _stealthUntil = 0;
            _stealthIcdUntil = frame + CombatStateRules.Frames(_rules.V.StealthReenterIcdSeconds);
            _events?.Publish(new OnStealthBreak(frame, 0, _id));
        }

        // ---------------------------------------------------------------- Parry / Intangible

        /// <summary>Opens the Parry window: base frames plus bonus, capped (06 3.8.3). Returns the last active frame.</summary>
        public int OpenParry(int frame, float bonusSeconds = 0f)
        {
            CurrentFrame = frame;
            int len = _rules.V.ParryWindowFrames + (bonusSeconds > 0f ? CombatStateRules.Frames(bonusSeconds) : 0);
            int cap = CombatStateRules.Frames(_rules.V.ParryCapSeconds);
            if (len > cap) len = cap;
            _parryStart = frame;
            _parryEnd = frame + len - 1;
            return _parryEnd;
        }

        /// <summary>Last active Parry frame, to mirror into <c>CombatEntity.ParryActiveUntilFrame</c>.</summary>
        public int ParryEndFrame => _parryEnd;
        public bool IsParryActive(int frame) => frame >= _parryStart && frame <= _parryEnd;

        /// <summary>
        /// True if an incoming hit is negated by the Parry window (never against Unblockable or Annihilation). The attacker
        /// (melee) then takes <paramref name="attackerStagger"/>; the OnParry event is published by CombatEntity.
        /// </summary>
        public bool TryParry(HitFlags flags, int frame, out float attackerStagger)
        {
            attackerStagger = 0f;
            if (!IsParryActive(frame) || (flags & (HitFlags.Unblockable | HitFlags.Annihilation)) != 0) return false;
            attackerStagger = _rules.V.ParryAttackerStagger;
            return true;
        }

        /// <summary>Intangible: displacement-skill i-frames, same semantics as the dodge i-frames (06 3.8.3).</summary>
        public void GrantIntangible(int startFrame, int endFrameInclusive, int sourceId = 0)
        {
            if (endFrameInclusive + 1 > _intangibleUntil) _intangibleUntil = endFrameInclusive + 1;
            Invulnerability?.AddWindow(startFrame, endFrameInclusive, 0, sourceId);
        }

        /// <summary>Passes through enemies (not walls).</summary>
        public bool IsIntangible(int frame) => frame < _intangibleUntil;

        // ---------------------------------------------------------------- Player hit reaction (06 3.8.4)

        public PlayerReaction CurrentReaction => _reaction;

        /// <summary>Starts the player's reaction to a hit; ignored under Super Armor or while a Knockdown is running.</summary>
        public PlayerReaction BeginPlayerReaction(PlayerHitWeight weight, bool knockdown, int frame)
        {
            CurrentFrame = frame;
            if (_reaction == PlayerReaction.Knockdown && frame < _reactionEnd) return PlayerReaction.None;
            PlayerReaction r = knockdown ? PlayerReaction.Knockdown
                : weight == PlayerHitWeight.Light ? PlayerReaction.Flash : PlayerReaction.Flinch;
            if (r != PlayerReaction.Flash && HasSuperArmor(frame)) return PlayerReaction.None;
            _reaction = r;
            _reactionStart = frame;
            if (r == PlayerReaction.Flinch) _reactionEnd = frame + _rules.V.PlayerFlinchFrames;
            else if (r == PlayerReaction.Knockdown)
            {
                _reactionEnd = frame + _rules.V.KnockdownFrames;
                // I-frames cover the last N frames of the fall.
                Invulnerability?.AddWindow(_reactionEnd - _rules.V.KnockdownInvulnerableFrames, _reactionEnd - 1);
            }
            else _reactionEnd = frame;
            return r;
        }

        /// <summary>A flinch can be cancelled by a dodge after the configured frame; a Knockdown cannot.</summary>
        public bool CanDodgeCancelReaction(int frame)
        {
            if (_reaction == PlayerReaction.Flinch && frame < _reactionEnd)
                return frame - _reactionStart >= _rules.V.PlayerFlinchCancelFrame;
            return _reaction != PlayerReaction.Knockdown || frame >= _reactionEnd;
        }
    }
}

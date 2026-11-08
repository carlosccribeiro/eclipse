using System;
using Eclipse.Core.Damage;
using Eclipse.Core.Random;
using Eclipse.Core.Stats;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Fills the player's <see cref="IncomingDefense"/> from its <see cref="StatBlock"/> and runs the Core
    /// <see cref="IncomingDamageResolver"/> (06 3.3). No damage math here: all of it lives in the resolver.
    /// </summary>
    public sealed class IncomingDamagePipeline
    {
        private readonly IncomingDamageResolver _resolver;
        private readonly IncomingDefense _defense = new IncomingDefense();
        private readonly RngStreams _rng;
        private readonly float _blockAmount;

        public IncomingDamagePipeline(CombatEntityConfig cfg, RngStreams rng)
        {
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _resolver = new IncomingDamageResolver(cfg.Mitigation);
            _blockAmount = cfg.BlockAmount;
        }

        public IncomingResult Resolve(DamageInstance hit, StatBlock stats, float shieldPoints, bool hasIFrames, bool parryActive,
            in IncomingContext ctx)
        {
            IncomingDefense d = _defense;
            d.Armor = stats.Evaluate(StatId.Armor);
            d.PhysicalResistance = stats.Evaluate(StatId.PhysicalResistance);
            d.ElementalResistance = stats.Evaluate(StatId.ElementalResistance);
            d.VoidResistance = stats.Evaluate(StatId.VoidResistance);
            d.DodgeChance = stats.Evaluate(StatId.DodgeChance);
            d.BlockChance = stats.Evaluate(StatId.BlockChance);
            d.BlockAmount = _blockAmount;
            d.DamageReduction = stats.Evaluate(StatId.DamageReduction);
            d.ShieldPoints = shieldPoints;
            d.HasIFrames = hasIFrames;
            d.ParryActive = parryActive;
            return _resolver.Resolve(hit, d, in ctx, _rng);
        }
    }
}

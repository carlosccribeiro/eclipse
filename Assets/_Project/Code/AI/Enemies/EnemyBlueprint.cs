using System;
using System.Collections.Generic;
using Eclipse.AI.Telegraphs;
using Eclipse.AI.Utility;
using Eclipse.Data.Enemies;

namespace Eclipse.AI.Enemies
{
    /// <summary>
    /// Everything the AI needs about one enemy type at one Monster Level and difficulty, derived from data once at spawn:
    /// absolute stats, the attack plans (combo heads and on-death attacks) and the Utility configuration whose
    /// <see cref="AttackOption"/>s mirror the plans (pending of T2: weights, range, <c>DurationSeconds</c> and
    /// <c>RecoverSeconds</c> now come from <see cref="AttackDefinition"/>).
    /// </summary>
    public sealed class EnemyBlueprint
    {
        public EnemyDefinition Definition { get; private set; }
        public EnemyStats Stats { get; private set; }
        public ArchetypeTemplate Template { get; private set; }
        /// <summary>Selectable attacks (combo heads); index matches <see cref="UtilityConfig.Attacks"/>.</summary>
        public AttackPlan[] Heads { get; private set; }
        /// <summary>Attacks fired when the enemy dies (Husk spark).</summary>
        public AttackPlan[] DeathPlans { get; private set; }
        public UtilityConfig Utility { get; private set; }

        /// <param name="windupReduction">Difficulty wind-up reduction fraction (capped by <see cref="WindupMath.MaxReduction"/>).</param>
        public static EnemyBlueprint Build(EnemyDefinition enemy, in EnemyStats stats, float windupReduction,
            WindupClass windupClass = WindupClass.Common, ArchetypeTemplate template = null)
        {
            if (enemy == null) throw new ArgumentNullException(nameof(enemy));
            template = template ?? ArchetypeTemplates.For(enemy);

            var chained = new HashSet<AttackDefinition>();
            for (int i = 0; i < enemy.Attacks.Count; i++)
            {
                AttackDefinition next = enemy.Attacks[i] != null ? enemy.Attacks[i].ChainNext : null;
                int guard = 0;
                while (next != null && guard++ < 8) { chained.Add(next); next = next.ChainNext; }
            }

            var heads = new List<AttackPlan>();
            var death = new List<AttackPlan>();
            for (int i = 0; i < enemy.Attacks.Count; i++)
            {
                AttackDefinition def = enemy.Attacks[i];
                if (def == null) continue;
                if (def.Trigger == AttackTrigger.OnDeath)
                {
                    death.Add(AttackPlan.Build(def, in stats, enemy.BaseElement, windupReduction, windupClass));
                    continue;
                }
                if (chained.Contains(def)) continue; // second and later strikes of a combo are reached through the chain
                heads.Add(AttackPlan.Build(def, in stats, enemy.BaseElement, windupReduction, windupClass));
            }

            var utility = new UtilityConfig { Attacks = new AttackOption[heads.Count] };
            template.Apply(utility, enemy);
            for (int i = 0; i < heads.Count; i++)
            {
                AttackPlan p = heads[i];
                utility.Attacks[i] = new AttackOption
                {
                    MinRange = p.MinRange,
                    MaxRange = p.MaxRange,
                    MaxAngleDeg = template.AttackAngleToleranceDeg,
                    Weight = p.Weight,
                    Ranged = p.Ranged,
                    DurationSeconds = p.ChainAttackSeconds,
                    RecoverSeconds = p.ChainRecoverSeconds,
                };
            }

            return new EnemyBlueprint
            {
                Definition = enemy,
                Stats = stats,
                Template = template,
                Heads = heads.ToArray(),
                DeathPlans = death.ToArray(),
                Utility = utility,
            };
        }
    }
}

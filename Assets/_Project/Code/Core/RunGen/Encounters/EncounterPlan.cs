using System.Collections.Generic;
using Eclipse.Core.Ids;

namespace Eclipse.Core.RunGen.Encounters
{
    /// <summary>One enemy of a planned wave.</summary>
    public readonly struct PlannedUnit
    {
        public readonly StableId EnemyId;
        public readonly EnemyArchetype Archetype;
        public readonly int Threat;
        public readonly bool IsElite;

        public PlannedUnit(StableId enemyId, EnemyArchetype archetype, int threat, bool isElite)
        {
            EnemyId = enemyId; Archetype = archetype; Threat = threat; IsElite = isElite;
        }
    }

    public sealed class PlannedWave
    {
        public readonly List<PlannedUnit> Units = new List<PlannedUnit>();
        /// <summary>PA aimed at for this wave (after carry-over and the live-PA cap).</summary>
        public int TargetThreat;
        public int Threat;
        public int Casters, Summoners, Tanks, Frontline, SpecialistThreat, EliteThreat;
    }

    /// <summary>Result of <see cref="EncounterPlanner.Plan"/>. Never exceeds the budget.</summary>
    public sealed class EncounterPlan
    {
        public string RecipeId;
        public int Row;
        public PlannedWave[] Waves = new PlannedWave[0];
        public int Units;
        public int TotalThreat;
        public int EliteThreat;
        /// <summary>Exact budget of 09 section 10.1 (e.g. 72.8).</summary>
        public double Budget;
        /// <summary>Whole PA spendable: floor(Budget).</summary>
        public int SpendableBudget;
        /// <summary>True when the budget could not be closed within the limits (PA was reduced, never exceeded).</summary>
        public bool UnderBudget;
        /// <summary>Elite Arena only: Elite PA over total PA reached the configured minimum.</summary>
        public bool EliteShareMet = true;
        /// <summary>Non-null when no plan was possible (e.g. pool without front line); Waves is then empty.</summary>
        public string Error;
    }
}

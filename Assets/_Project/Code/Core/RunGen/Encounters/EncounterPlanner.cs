using System;
using System.Collections.Generic;
using Eclipse.Core.Loot;
using Eclipse.Core.Random;
using Eclipse.Core.RunGen.Map;

namespace Eclipse.Core.RunGen.Encounters
{
    /// <summary>Input of <see cref="EncounterPlanner.Plan(EncounterRequest, ref Xoshiro128StarStar)"/>.</summary>
    public sealed class EncounterRequest
    {
        /// <summary>Map row, 1-based (09 section 10.1).</summary>
        public int Row = 1;
        /// <summary>Only <see cref="RoomType.Elite"/> changes the plan (Elite Arena); other types plan as plain combat.</summary>
        public RoomType RoomType = RoomType.Combat;
        public Difficulty Difficulty = Difficulty.Normal;
        /// <summary>1-100, only used when Difficulty is Ascension.</summary>
        public int AscensionLevel;
        /// <summary>Region id for recipe weights; empty matches recipes with an empty region.</summary>
        public string RegionId = "";
        /// <summary>09 section 10.1 MultRegiao (1.0 everywhere today).</summary>
        public float RegionMult = 1f;
        /// <summary>Enemies of the region. Elite-flagged candidates are only used by Elite Arena.</summary>
        public IReadOnlyList<EnemyCandidate> Pool;
        /// <summary>Forces a recipe by id; null/empty draws one by weight for (region, row).</summary>
        public string RecipeId;
    }

    /// <summary>
    /// Turns a room request into an <see cref="EncounterPlan"/> (09 section 10). Deterministic for a given
    /// generator state; uses only the Encounter stream (never System.Random).
    ///
    /// Guarantees (per wave, enforced by construction, not by luck): every add passes a single gate
    /// (<c>Can</c>) that checks live units, live PA (also the budget remainder, so the budget is never
    /// exceeded), caster/summoner/tank caps and the specialist share against the wave target; a front line
    /// unit is reserved first and later adds keep room for it; a final repair removes specialists until the
    /// share holds for the real wave PA; a wave without front line is dropped. Budget that cannot be spent
    /// within the limits is left unspent and flagged <see cref="EncounterPlan.UnderBudget"/>.
    /// </summary>
    public sealed class EncounterPlanner
    {
        private const double Eps = 1e-9;
        private const int ArchetypeCount = 8;
        private readonly EncounterConfig _cfg;

        public EncounterPlanner() : this(EncounterConfig.Default()) { }

        public EncounterPlanner(EncounterConfig config)
        {
            _cfg = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>Plans with the Encounter stream of a run.</summary>
        public EncounterPlan Plan(EncounterRequest request, RngStreams streams) =>
            Plan(request, ref streams.Get(RngStream.Encounter));

        /// <summary>Plans with a self-contained generator seeded by <paramref name="seed"/> (tests, previewer).</summary>
        public EncounterPlan Plan(EncounterRequest request, ulong seed)
        {
            var rng = Xoshiro128StarStar.FromSeed(seed);
            return Plan(request, ref rng);
        }

        public EncounterPlan Plan(EncounterRequest request, ref Xoshiro128StarStar rng)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Pool == null) throw new ArgumentNullException(nameof(request.Pool));
            if (request.Row < 1) throw new ArgumentOutOfRangeException(nameof(request), "Row must be >= 1.");

            bool arena = request.RoomType == RoomType.Elite;
            var plan = new EncounterPlan { Row = request.Row };
            plan.Budget = ThreatBudget.Compute(_cfg, request.Row, request.Difficulty, request.AscensionLevel, request.RegionMult, arena);
            plan.SpendableBudget = ThreatBudget.Spendable(plan.Budget);

            var regular = new List<EnemyCandidate>();
            var elites = new List<EnemyCandidate>();
            for (int i = 0; i < request.Pool.Count; i++)
            {
                var c = request.Pool[i];
                if (c == null || c.Threat <= 0 || c.RegionWeight <= 0f) continue;
                if (c.IsElite) elites.Add(c); else regular.Add(c);
            }
            var all = new List<EnemyCandidate>(regular);
            if (arena) all.AddRange(elites);

            EncounterRecipe recipe = ChooseRecipe(request, ref rng);
            plan.RecipeId = recipe?.Id;

            bool hasFront = false;
            for (int i = 0; i < all.Count; i++) if (all[i].IsFrontline) { hasFront = true; break; }
            if (!hasFront)
            {
                plan.Error = "Pool has no front line candidate.";
                plan.UnderBudget = plan.SpendableBudget > 0;
                plan.EliteShareMet = !arena;
                return plan;
            }

            float[] split = request.Row <= _cfg.TwoWaveLastRow ? _cfg.TwoWaveSplit : _cfg.ThreeWaveSplit;
            var waves = new List<PlannedWave>(split.Length);
            double cumulative = 0;
            int spent = 0;
            for (int w = 0; w < split.Length; w++)
            {
                cumulative = w == split.Length - 1 ? plan.Budget : cumulative + plan.Budget * split[w];
                // Carry-over: what earlier waves left unspent (live-PA cap, unit cap) is offered to this wave.
                int target = ThreatBudget.Spendable(cumulative) - spent;
                if (target > _cfg.Limits.MaxLivePa) target = _cfg.Limits.MaxLivePa;
                if (target < 1) continue;

                var builder = new WaveBuilder(_cfg, request.Row, recipe, regular, elites, all, arena, target);
                PlannedWave wave = builder.Build(ref rng);
                if (wave.Units.Count == 0) continue;
                waves.Add(wave);
                spent += wave.Threat;
            }

            plan.Waves = waves.ToArray();
            for (int w = 0; w < plan.Waves.Length; w++)
            {
                plan.Units += plan.Waves[w].Units.Count;
                plan.TotalThreat += plan.Waves[w].Threat;
                plan.EliteThreat += plan.Waves[w].EliteThreat;
            }
            plan.UnderBudget = plan.TotalThreat < plan.SpendableBudget;
            plan.EliteShareMet = !arena || (plan.TotalThreat > 0 && plan.EliteThreat >= _cfg.EliteArenaMinEliteShare * plan.TotalThreat - Eps);
            return plan;
        }

        private EncounterRecipe ChooseRecipe(EncounterRequest r, ref Xoshiro128StarStar rng)
        {
            var recipes = _cfg.Recipes;
            if (!string.IsNullOrEmpty(r.RecipeId))
            {
                for (int i = 0; i < recipes.Count; i++) if (recipes[i].Id == r.RecipeId) return recipes[i];
                throw new ArgumentException("Unknown recipe id: " + r.RecipeId, nameof(r));
            }
            float sum = 0f;
            for (int i = 0; i < recipes.Count; i++) sum += recipes[i].WeightFor(r.RegionId, r.Row);
            if (sum <= 0f) return null; // no recipe: uniform filler
            float roll = rng.NextFloat01() * sum;
            EncounterRecipe last = null;
            for (int i = 0; i < recipes.Count; i++)
            {
                float wt = recipes[i].WeightFor(r.RegionId, r.Row);
                if (wt <= 0f) continue;
                last = recipes[i];
                roll -= wt;
                if (roll < 0f) return last;
            }
            return last;
        }

        /// <summary>Mutable state of one wave. Every unit enters through <see cref="TryPick"/>, which applies <see cref="Can"/>.</summary>
        private sealed class WaveBuilder
        {
            private readonly EncounterConfig _cfg;
            private readonly EncounterLimits _lim;
            private readonly EncounterRecipe _recipe;
            private readonly List<EnemyCandidate> _regular, _elites, _all;
            private readonly bool _arena;
            private readonly int _target;
            private readonly int _casterCap, _summonerCap, _tankCap;
            private readonly int _minFrontThreat;
            private readonly PlannedWave _wave = new PlannedWave();
            private readonly int[] _count = new int[ArchetypeCount];
            private readonly int[] _threatBy = new int[ArchetypeCount];

            public WaveBuilder(EncounterConfig cfg, int row, EncounterRecipe recipe, List<EnemyCandidate> regular,
                List<EnemyCandidate> elites, List<EnemyCandidate> all, bool arena, int target)
            {
                _cfg = cfg; _lim = cfg.Limits; _recipe = recipe; _regular = regular; _elites = elites; _all = all;
                _arena = arena; _target = target;
                _casterCap = recipe != null && recipe.MaxCastersOverride > 0 ? recipe.MaxCastersOverride : _lim.MaxCastersPerWave;
                _summonerCap = recipe != null && recipe.MaxSummonersOverride > 0 ? recipe.MaxSummonersOverride : _lim.MaxSummonersPerWave;
                _tankCap = _lim.MaxTanks(row);
                int min = int.MaxValue;
                for (int i = 0; i < all.Count; i++) if (all[i].IsFrontline && all[i].Threat < min) min = all[i].Threat;
                _minFrontThreat = min;
                _wave.TargetThreat = target;
            }

            private int Remaining => _target - _wave.Threat;

            public PlannedWave Build(ref Xoshiro128StarStar rng)
            {
                // 1. Elite pack: at least the configured share of the wave PA (Elite Arena only).
                if (_arena)
                {
                    int goal = (int)Math.Ceiling(_cfg.EliteArenaMinEliteShare * _target - Eps);
                    while (_wave.EliteThreat < goal && TryPick(_elites, -1, false, true, ref rng)) { }
                }

                // 2. Front line guarantee (09 section 10.3).
                while (_wave.Frontline < _lim.MinFrontlinePerWave && TryPick(_all, -2, false, true, ref rng)) { }
                if (_wave.Frontline < _lim.MinFrontlinePerWave) { Clear(); return _wave; }

                // 3. Recipe slots (09 section 10.4).
                if (_recipe != null)
                {
                    for (int s = 0; s < _recipe.Slots.Count; s++)
                    {
                        var slot = _recipe.Slots[s];
                        int a = (int)slot.Archetype;
                        while (_count[a] < slot.MinCount && TryPick(_regular, a, false, true, ref rng)) { }
                        if (slot.TargetShare > 0f)
                        {
                            while (_threatBy[a] < slot.TargetShare * _target - Eps
                                   && (slot.MaxCount <= 0 || _count[a] < slot.MaxCount)
                                   && TryPick(_regular, a, false, true, ref rng)) { }
                        }
                    }
                }

                // 4. Filler up to the wave target; ignoring filler weights only when they leave nothing pickable.
                Fill(true, ref rng);

                // 5. Specialist share must hold for the real wave PA: remove specialists, refill without them.
                while (_wave.SpecialistThreat > _lim.MaxSpecialistShare * _wave.Threat + Eps)
                {
                    if (!RemoveSpecialist()) break;
                    Fill(false, ref rng);
                }
                return _wave;
            }

            private void Fill(bool allowSpecialist, ref Xoshiro128StarStar rng)
            {
                while (Remaining > 0)
                {
                    if (TryPick(_regular, -1, true, allowSpecialist, ref rng)) continue;
                    if (TryPick(_regular, -1, false, allowSpecialist, ref rng)) continue;
                    break;
                }
            }

            /// <summary>
            /// The single gate of 09 section 10.3 plus budget remainder. <paramref name="allowSpecialist"/> is false
            /// during the repair refill.
            /// </summary>
            private bool Can(EnemyCandidate c, bool allowSpecialist)
            {
                int t = c.Threat;
                if (_wave.Units.Count >= _lim.MaxLiveUnits) return false;
                if (t > Remaining) return false;                       // budget never exceeded
                if (_wave.Threat + t > _lim.MaxLivePa) return false;
                switch (c.Archetype)
                {
                    case EnemyArchetype.Caster: if (_wave.Casters >= _casterCap) return false; break;
                    case EnemyArchetype.Summoner: if (_wave.Summoners >= _summonerCap) return false; break;
                    case EnemyArchetype.Tank: if (_wave.Tanks >= _tankCap) return false; break;
                }
                if (c.IsSpecialist)
                {
                    if (!allowSpecialist) return false;
                    if (_wave.SpecialistThreat + t > _lim.MaxSpecialistShare * _target + Eps) return false;
                }
                // Keep room for the mandatory front line unit.
                if (_wave.Frontline < _lim.MinFrontlinePerWave && !c.IsFrontline
                    && (_minFrontThreat == int.MaxValue || t > Remaining - _minFrontThreat)) return false;
                return true;
            }

            /// <summary>
            /// Weighted pick among <paramref name="list"/>. archetype -1 = any, -2 = any front line.
            /// When few unit slots remain, only candidates able to still close the PA remainder are kept.
            /// </summary>
            private bool TryPick(List<EnemyCandidate> list, int archetype, bool useFiller, bool allowSpecialist, ref Xoshiro128StarStar rng)
            {
                int slotsLeft = _lim.MaxLiveUnits - _wave.Units.Count;
                if (slotsLeft <= 0 || Remaining <= 0) return false;
                int need = (Remaining + slotsLeft - 1) / slotsLeft;

                int maxT = 0;
                for (int i = 0; i < list.Count; i++)
                    if (Eligible(list[i], archetype, useFiller, allowSpecialist) > 0f && list[i].Threat > maxT) maxT = list[i].Threat;
                if (maxT == 0) return false;
                if (need > maxT) need = maxT;

                float sum = 0f;
                for (int i = 0; i < list.Count; i++)
                    if (list[i].Threat >= need) sum += Eligible(list[i], archetype, useFiller, allowSpecialist);
                if (sum <= 0f) return false;

                float roll = rng.NextFloat01() * sum;
                EnemyCandidate chosen = null;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Threat < need) continue;
                    float wt = Eligible(list[i], archetype, useFiller, allowSpecialist);
                    if (wt <= 0f) continue;
                    chosen = list[i];
                    roll -= wt;
                    if (roll < 0f) break;
                }
                Add(chosen);
                return true;
            }

            /// <summary>Pick weight of a candidate (0 = not pickable now).</summary>
            private float Eligible(EnemyCandidate c, int archetype, bool useFiller, bool allowSpecialist)
            {
                if (archetype == -2) { if (!c.IsFrontline) return 0f; }
                else if (archetype >= 0 && (int)c.Archetype != archetype) return 0f;
                float w = c.RegionWeight;
                if (useFiller && _recipe != null) w *= _recipe.FillerWeight(c.Archetype);
                if (w <= 0f) return 0f;
                return Can(c, allowSpecialist) ? w : 0f;
            }

            private void Add(EnemyCandidate c)
            {
                _wave.Units.Add(new PlannedUnit(c.Id, c.Archetype, c.Threat, c.IsElite));
                Apply(c.Archetype, c.Threat, c.IsElite, 1);
            }

            private bool RemoveSpecialist()
            {
                int idx = -1;
                for (int i = _wave.Units.Count - 1; i >= 0; i--)
                {
                    var u = _wave.Units[i];
                    if (!EnemyCandidate.ArchetypeIsSpecialist(u.Archetype)) continue;
                    if (!u.IsElite) { idx = i; break; }
                    if (idx < 0) idx = i; // fall back to the last Elite specialist
                }
                if (idx < 0) return false;
                var r = _wave.Units[idx];
                _wave.Units.RemoveAt(idx);
                Apply(r.Archetype, r.Threat, r.IsElite, -1);
                return true;
            }

            private void Apply(EnemyArchetype a, int threat, bool elite, int sign)
            {
                _count[(int)a] += sign;
                _threatBy[(int)a] += sign * threat;
                _wave.Threat += sign * threat;
                if (a == EnemyArchetype.Caster) _wave.Casters += sign;
                if (a == EnemyArchetype.Summoner) _wave.Summoners += sign;
                if (a == EnemyArchetype.Tank) _wave.Tanks += sign;
                if (EnemyCandidate.ArchetypeIsFrontline(a)) _wave.Frontline += sign;
                if (EnemyCandidate.ArchetypeIsSpecialist(a)) _wave.SpecialistThreat += sign * threat;
                if (elite) _wave.EliteThreat += sign * threat;
            }

            private void Clear()
            {
                _wave.Units.Clear();
                Array.Clear(_count, 0, _count.Length);
                Array.Clear(_threatBy, 0, _threatBy.Length);
                _wave.Threat = 0; _wave.Casters = 0; _wave.Summoners = 0; _wave.Tanks = 0;
                _wave.Frontline = 0; _wave.SpecialistThreat = 0; _wave.EliteThreat = 0;
            }
        }
    }
}

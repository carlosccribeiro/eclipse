using System;
using Eclipse.Core.Ids;
using Eclipse.Gameplay.Simulation;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Data of one stub status: a stack counter with a renewable duration (<c>StackIntensity(max)</c>).
    /// Plain data so a future ScriptableObject/CSV (M11) can fill it; no balance value lives in the runtime code.
    /// </summary>
    [Serializable]
    public struct StatusStubDefinition
    {
        /// <summary>Slug of the status (StableId needs at least two segments), e.g. "status.burning".</summary>
        public string StatusId;
        public int MaxStacks;
        /// <summary>Duration in ticks used when the request has none; renewed on every application.</summary>
        public int DefaultDurationFrames;
        /// <summary>Damage per stack per second as a fraction of the source DRef (0.08 = 8%); 0 for non-DoT statuses.</summary>
        public float DotFractionPerStackPerSecond;

        public StatusStubDefinition(string statusId, int maxStacks, int defaultDurationFrames, float dotFractionPerStackPerSecond)
        {
            StatusId = statusId; MaxStacks = maxStacks; DefaultDurationFrames = defaultDurationFrames;
            DotFractionPerStackPerSecond = dotFractionPerStackPerSecond;
        }

        /// <summary>
        /// PROVISIONAL Burning (07 section 4): 4 s, max 10 stacks, 8% DRef Ember per stack per second, duration renewed per
        /// stack, application guaranteed (no buildup). Replaced by the M11 status system; the numbers are the doc's, not tuning.
        /// </summary>
        public static StatusStubDefinition BurningProvisional() =>
            new StatusStubDefinition("status.burning", 10, 4 * SimulationFrame.TicksPerSecond, 0.08f);
    }

    /// <summary>
    /// Provisional <see cref="IStatusReceiver"/> (M05). Keeps stacks + remaining duration per configured status;
    /// no buildup, no resistance, no interactions (M11 replaces it behind the same contract). Plain C#, no allocation per tick.
    /// </summary>
    public sealed class StatusStub : IStatusReceiver, IFixedTickable
    {
        private readonly StatusStubDefinition[] _defs;
        private readonly StableId[] _ids;
        private readonly int[] _stacks;
        private readonly int[] _remaining;
        private readonly EntityId _id;
        private readonly CombatEventStream _events;
        private int _frame;

        public StatusStub(EntityId id, StatusStubDefinition[] definitions, CombatEventStream events = null)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            _id = id;
            _events = events;
            _defs = (StatusStubDefinition[])definitions.Clone();
            _ids = new StableId[_defs.Length];
            for (int i = 0; i < _defs.Length; i++) _ids[i] = new StableId(_defs[i].StatusId);
            _stacks = new int[_defs.Length];
            _remaining = new int[_defs.Length];
        }

        public EntityId Id => _id;

        private int IndexOf(StableId status)
        {
            for (int i = 0; i < _ids.Length; i++)
                if (_ids[i] == status) return i;
            return -1;
        }

        public bool TryApply(in StatusRequest request)
        {
            int i = IndexOf(request.Status);
            if (i < 0 || request.Stacks <= 0) return false;
            int max = _defs[i].MaxStacks;
            int before = _stacks[i];
            _stacks[i] = Math.Min(max, before + request.Stacks);
            _remaining[i] = request.DurationFrames > 0 ? request.DurationFrames : _defs[i].DefaultDurationFrames;
            _events?.Publish(new OnStatusApplied(_frame, request.Generation, request.Source, _id, request.Status, _stacks[i]));
            return true;
        }

        public int GetStacks(StableId status)
        {
            int i = IndexOf(status);
            return i < 0 ? 0 : _stacks[i];
        }

        public int GetRemainingFrames(StableId status)
        {
            int i = IndexOf(status);
            return i < 0 || _stacks[i] == 0 ? 0 : _remaining[i];
        }

        public void Remove(StableId status)
        {
            int i = IndexOf(status);
            if (i < 0 || _stacks[i] == 0) return;
            _stacks[i] = 0;
            _remaining[i] = 0;
        }

        /// <summary>Total DoT strength of a status as a fraction of DRef per second (stacks x per-stack fraction).</summary>
        public float GetDotFractionPerSecond(StableId status)
        {
            int i = IndexOf(status);
            return i < 0 ? 0f : _stacks[i] * _defs[i].DotFractionPerStackPerSecond;
        }

        public void Tick(in SimulationFrame frame)
        {
            _frame = frame.Index;
            for (int i = 0; i < _stacks.Length; i++)
            {
                if (_stacks[i] == 0) continue;
                if (--_remaining[i] > 0) continue;
                _stacks[i] = 0;
                _remaining[i] = 0;
                _events?.Publish(new OnStatusExpired(_frame, 0, _id, _ids[i]));
            }
        }
    }
}

namespace Eclipse.Gameplay.Input
{
    /// <summary>
    /// Input buffer measured in simulation frames, never wall-clock time (06 §3.4: 9 frames = 150 ms at 60 Hz).
    /// An entry pushed at frame F is consumable while <c>currentFrame - F &lt; BufferFrames</c>, i.e. frames F..F+8;
    /// at F+9 it has expired. Consumption picks the highest priority (Dodge > Ability > Heavy > Basic); within
    /// the same priority the most recent press wins (higher frame, then higher insertion sequence).
    /// Fixed capacity, zero allocation after construction.
    /// </summary>
    public sealed class InputBuffer
    {
        /// <summary>06 §3.4 / M04 T3: buffer window in frames at 60 Hz.</summary>
        public const int BufferFrames = 9;
        private const int Capacity = 16;

        private readonly BufferedAction[] _items = new BufferedAction[Capacity];
        private int _count;
        private int _sequence;

        public int Count => _count;

        /// <summary>Records a press. Non-buffered actions (priority &lt; 0) are ignored. When full, the oldest entry is dropped.</summary>
        public void Push(InputActionId id, int frame)
        {
            if (id.BufferPriority() < 0) return;
            Purge(frame);
            if (_count == Capacity) RemoveAt(0);
            _items[_count++] = new BufferedAction(id, frame, ++_sequence);
        }

        /// <summary>True if an unexpired entry of <paramref name="id"/> exists.</summary>
        public bool Contains(InputActionId id, int currentFrame)
        {
            for (int i = 0; i < _count; i++)
                if (_items[i].Id == id && currentFrame - _items[i].Frame < BufferFrames) return true;
            return false;
        }

        public bool TryConsume(int currentFrame, out BufferedAction action) =>
            TryConsume(currentFrame, InputActionIdExtensions.AllMask, out action);

        /// <summary>Removes and returns the best unexpired entry among those whose bit is set in <paramref name="acceptedMask"/>.</summary>
        public bool TryConsume(int currentFrame, uint acceptedMask, out BufferedAction action)
        {
            Purge(currentFrame);
            int best = -1;
            for (int i = 0; i < _count; i++)
            {
                var c = _items[i];
                if ((acceptedMask & c.Id.Bit()) == 0) continue;
                if (best < 0 || IsBetter(c, _items[best])) best = i;
            }
            if (best < 0)
            {
                action = default;
                return false;
            }
            action = _items[best];
            RemoveAt(best);
            return true;
        }

        public void Clear()
        {
            _count = 0;
        }

        private static bool IsBetter(BufferedAction a, BufferedAction b)
        {
            int pa = a.Priority, pb = b.Priority;
            if (pa != pb) return pa > pb;
            if (a.Frame != b.Frame) return a.Frame > b.Frame;
            return a.Sequence > b.Sequence;
        }

        private void Purge(int currentFrame)
        {
            int w = 0;
            for (int r = 0; r < _count; r++)
                if (currentFrame - _items[r].Frame < BufferFrames) _items[w++] = _items[r];
            _count = w;
        }

        private void RemoveAt(int index)
        {
            for (int i = index; i < _count - 1; i++) _items[i] = _items[i + 1];
            _count--;
        }
    }
}

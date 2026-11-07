namespace Eclipse.Gameplay.Input
{
    /// <summary>One buffered press: what, when (simulation frame) and insertion order (tie-break).</summary>
    public readonly struct BufferedAction
    {
        public readonly InputActionId Id;
        public readonly int Frame;
        public readonly int Sequence;

        public BufferedAction(InputActionId id, int frame, int sequence)
        {
            Id = id;
            Frame = frame;
            Sequence = sequence;
        }

        public int Priority => Id.BufferPriority();
        public bool IsValid => Id != InputActionId.None;
    }
}

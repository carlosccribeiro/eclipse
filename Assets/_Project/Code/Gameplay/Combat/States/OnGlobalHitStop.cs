using Eclipse.Gameplay.Combat;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Request for a world-wide hit-stop (Broken: 6 f, 06 section 3.8.2). Published on the combat event stream; the
    /// feedback layer (M05 T9) decides how to freeze. Gameplay never freezes the world by itself.
    /// </summary>
    public readonly struct OnGlobalHitStop
    {
        public readonly int Frame, Generation;
        public readonly EntityId Target;
        public readonly int Frames;

        public OnGlobalHitStop(int frame, int generation, EntityId target, int frames)
        { Frame = frame; Generation = generation; Target = target; Frames = frames; }
    }
}

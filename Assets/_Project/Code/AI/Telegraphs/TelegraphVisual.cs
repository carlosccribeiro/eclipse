using Eclipse.Data.Enemies;

namespace Eclipse.AI.Telegraphs
{
    /// <summary>
    /// Visual kinds of the telegraph language (06 section 3.10, R14). The first six values mirror
    /// <see cref="TelegraphType"/> one to one; the rest are AI-only visuals. There is no orange entry on purpose:
    /// orange/yellow on the ground is reserved to Ember (the Annihilation dashed border is the one yellow exception).
    /// </summary>
    public enum TelegraphVisual : byte
    {
        Red = 0,
        Purple = 1,
        WhiteGold = 2,
        CyanRing = 3,
        Annihilation = 4,
        UnstoppableOutline = 5,
        SniperLine = 6,
        Grab = 7,
        EdgeIndicator = 8,
    }

    public static class TelegraphVisuals
    {
        public const int Count = 9;

        /// <summary>Maps the data enum to its visual. A thin Line projectile attack uses SniperLine (see WindupMath.IsSniper).</summary>
        public static TelegraphVisual From(TelegraphType type) => (TelegraphVisual)(byte)type;
    }
}

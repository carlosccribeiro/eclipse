namespace Eclipse.Core.RunGen.Map
{
    /// <summary>One room of the run map (09 §11.1). Plain data; the graph lives in <see cref="RunMap"/>.</summary>
    public sealed class MapNode
    {
        /// <summary>1-based row (1..13, Boss = 14).</summary>
        public int Row;

        /// <summary>0-based column inside the row; rows are ordered left to right (edges never cross).</summary>
        public int Column;

        public RoomType Type;

        /// <summary>True for the fixed Rest node of row 13 (09 §11.1).</summary>
        public bool IsAntechamber;

        /// <summary>Door reward of a Combat node (09 §9); <see cref="DoorReward.None"/> otherwise.</summary>
        public DoorReward Reward;

        /// <summary>Event id drawn from the context pool (Event nodes). Empty when none was available.</summary>
        public string EventId = string.Empty;

        /// <summary>Reserved for Room Modifiers (09 §9.2); always empty until M20/M29.</summary>
        public string RoomModifierId = string.Empty;

        /// <summary>Column indices (in the next row) this node connects to: 1-2 consecutive values, ascending.</summary>
        public int[] Next = new int[0];
    }
}

using RabbitVsMole;

namespace PlayerManagementSystem.Backpack.Events
{
    public struct InventoryChangedEvent
    {
        public PlayerType PlayerType;
        public int Count;
        public int Capacity;
        public int Diff;
        public BackpackItemType BackpackItemType;

        public InventoryChangedEvent(PlayerType playerType, BackpackItemType backpackItemType, int count, int capacity, int diff)
        {
            PlayerType = playerType;
            Count = count;
            Capacity = capacity;
            BackpackItemType = backpackItemType;
            Diff = diff;
        }

        // Backwards-compatible ctor for any legacy call sites; defaults PlayerType to 0.
        public InventoryChangedEvent(BackpackItemType backpackItemType, int count, int capacity, int diff)
            : this(default, backpackItemType, count, capacity, diff) { }
    }
}


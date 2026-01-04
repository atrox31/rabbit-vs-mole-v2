namespace PlayerManagementSystem.Backpack.Events
{
    public struct InventoryChangedEvent
    {
        public int Count;
        public int Capacity;
        public int Diff;
        public BackpackItemType BackpackItemType;

        public InventoryChangedEvent(BackpackItemType backpackItemType, int count, int capacity, int diff)
        {
            Count = count;
            Capacity = capacity;
            BackpackItemType = backpackItemType;
            Diff = diff;
        }
    }
}


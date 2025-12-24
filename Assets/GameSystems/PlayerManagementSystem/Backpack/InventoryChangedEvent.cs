namespace PlayerManagementSystem.Backpack
{
    public struct InventoryChangedEvent
    {
        public int Count;
        public int Capacity;
        public BackpackItemType BackpackItemType;

        public InventoryChangedEvent(BackpackItemType backpackItemType, int count, int capacity)
        {
            Count = count;
            Capacity = capacity;
            BackpackItemType = backpackItemType;
        }
    }
}


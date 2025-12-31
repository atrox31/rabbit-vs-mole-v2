namespace PlayerManagementSystem.Backpack.Events
{
    public struct InventoryErrorEvent
    {
        public BackpackItemType BackpackItemType;

        public InventoryErrorEvent(BackpackItemType backpackItemType)
        {
            BackpackItemType = backpackItemType;
        }
    }
}


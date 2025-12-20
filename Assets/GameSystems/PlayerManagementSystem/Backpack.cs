using Unity.VisualScripting;
namespace PlayerManagementSystem
{
    public class Backpack
    {
        public BackpackItem Seed { get; private set; } = new BackpackItem("Seed", capacity: 3);
        public BackpackItem Water { get; private set; } = new BackpackItem("Water", capacity: 12);
        public BackpackItem Dirt { get; private set; } = new BackpackItem("Dirt", capacity: 5);
    }

    public class BackpackItem
    {
        public string Name { get; protected set; } // mayby delete this, idn, we will see
        public int Count { get; protected set; }
        public int Capacity { get; protected set; }
        public bool TryGet(int value = 1)
        {
            var newCount = Count - value;
            if (newCount < 0)
                return false;
            Count = newCount;
            return true;
        }
        public bool TryInsert(int value = 1, bool rejectTheSuperstate = true)
        {
            if (Count == Capacity)
                return false;

            var newCount = Count + value;
            if (newCount > Capacity)
            {
                if (rejectTheSuperstate)
                {
                    Count = Capacity;
                    return true;
                }
                return false;
            }
            Count = newCount;
            return true;
        }
        public BackpackItem(string name, int capacity)
        {
            Name = name;
            Capacity = capacity;
            Count = 0;
        }
    }
}
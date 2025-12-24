using GameSystems;
using RabbitVsMole;
using RabbitVsMole.GameData;
using System;
using UnityEngine;

namespace PlayerManagementSystem.Backpack
{
    public class BackpackItem
    {
        public BackpackItemType ItemType { get; private set; }
        public PlayerType PlayerType { get; private set; }
        public int Count { get; protected set; }
        public int Capacity { get; protected set; }
        private void SendChangeEvent() =>
            EventBus.Publish(new InventoryChangedEvent(ItemType, Count, Capacity));

        public bool CanInsert(int value = 1, bool rejectTheSuperstate = true) =>
            rejectTheSuperstate
            ? Count < Capacity
            : Count + value <= Capacity;

        public bool CanGet(int value = 1) => 
            Count >= value;

        public bool TryGet(int value = 1)
        {
            var newCount = Count - value;
            if (newCount < 0)
                return false;
            Count = newCount;
            SendChangeEvent();
            return true;
        }
        
        public bool TryInsert(int value = 1, bool rejectTheSuperstate = true)
        {
            var capacity = Capacity;
            if (Count == capacity)
                return false;

            var newCount = Count + value;
            if (newCount > capacity)
            {
                if (rejectTheSuperstate)
                {
                    Count = capacity;
                    SendChangeEvent();
                    return true;
                }
                return false;
            }
            Count = newCount;
            SendChangeEvent();
            return true;
        }
        
        public BackpackItem(PlayerType playerType, BackpackItemType itemType, int capacity)
        {
            PlayerType = playerType;
            ItemType = itemType;
            Count = 0;
            Capacity = capacity;
        }
    }
}


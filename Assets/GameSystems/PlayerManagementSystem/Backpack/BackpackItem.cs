using GameSystems;
using PlayerManagementSystem.Backpack.Events;
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

        public bool IsEmpty => Count == 0;
        public bool IsFull => Count == Capacity;
        private void SendChangeEvent() =>
            EventBus.Publish(new InventoryChangedEvent(ItemType, Count, Capacity));
        private void SendErrorEvent() =>
            EventBus.Publish(new InventoryErrorEvent(ItemType));

        public bool CanInsert(int value = 1, bool rejectTheSuperstate = true) {
            var answer = rejectTheSuperstate
                ? Count<Capacity
                : Count + value <= Capacity;

            if (!answer)
                SendErrorEvent();
            return answer;
        }

        public bool CanGet(int value = 1)
        {
            var answer = Count >= value;
            if(!answer)
                SendErrorEvent();
            return answer;
        }

        public bool TryGet(int value = 1)
        {
            var newCount = Count - value;
            if (newCount < 0)
                return false;
            Count = newCount;
            SendChangeEvent();
            return true;
        }

        public int GetAll()
        {
            var count = Count;
            Count = 0;
            return count;
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
        
        public BackpackItem(PlayerType playerType, BackpackItemType itemType, int capacity, bool startsFull = false)
        {
            PlayerType = playerType;
            ItemType = itemType;
            Count = startsFull ? capacity : 0;
            Capacity = capacity;
        }
    }
}


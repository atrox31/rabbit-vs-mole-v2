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
        private void SendChangeEvent(int diff) =>
            EventBus.Publish(new InventoryChangedEvent(ItemType, Count, Capacity, diff));
        private void SendErrorEvent() =>
            EventBus.Publish(new InventoryErrorEvent(ItemType));

        public bool CanInsert(int value = 1, bool rejectTheSuperstate = true)
        {
            bool canFit = rejectTheSuperstate
                ? Count < Capacity
                : Count + value <= Capacity;

            if (!canFit)
            {
                SendErrorEvent();
                return false;
            }
            return true;
        }

        public bool CanGet(int value = 1)
        {
            if (Count >= value)
                return true;

            SendErrorEvent();
            return false;
        }

        public bool TryGet(int value = 1)
        {
            if (value <= 0) 
                return false;

            if (Count < value)
                return false;

            Count -= value;
            SendChangeEvent(-value);
            return true;
        }

        public int GetAll()
        {
            if (Count == 0) return 0;

            int removedAmount = Count;
            Count = 0;

            SendChangeEvent(-removedAmount);
            return removedAmount;
        }

        public bool TryInsert(int value = 1, bool rejectTheSuperstate = true)
        {
            // 1. Initial check: Is there any space at all?
            if (Count >= Capacity || value <= 0)
                return false;

            // 2. Scenario: Full value fits easily
            if (Count + value <= Capacity)
            {
                Count += value;
                SendChangeEvent(value);
                return true;
            }

            // 3. Scenario: Not enough space for the full value
            // If rejectTheSuperstate is true, we fill the remaining space.
            // If false, we don't insert anything and return false.
            if (rejectTheSuperstate)
            {
                int spaceLeft = Capacity - Count;
                Count = Capacity;

                // Report only the actual amount added
                SendChangeEvent(spaceLeft);
                return true;
            }

            // Full value didn't fit and partial insert is not allowed
            return false;
        }

        /// <summary>
        /// Network/authority update: force set count and publish a single InventoryChangedEvent diff.
        /// </summary>
        public void SetCountFromNetwork(int newCount)
        {
            newCount = Mathf.Clamp(newCount, 0, Capacity);
            int old = Count;
            if (old == newCount)
                return;

            Count = newCount;
            SendChangeEvent(newCount - old);
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


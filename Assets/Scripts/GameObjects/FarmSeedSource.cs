using Enums;
using RabbitVsMole;
using System;
using UnityEngine;

namespace GameObjects
{
    public class FarmSeedSource : MonoBehaviour, IInteractable
    {
        public void Interact(PlayerType type, Func<ActionType, bool> setActionType, Action<bool> changeIsBusy)
        {
            return;
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}

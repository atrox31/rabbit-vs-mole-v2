using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Base;
using RabbitVsMole.InteractableGameObject.Enums;
using RabbitVsMole.InteractableGameObject.Visuals;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitVsMole.InteractableGameObject.Storages
{
    public class UndergroundCarrotStorage : StorageBase
    {
        [Header("Visuals")]
        [SerializeField] protected CarrotModelInStorage _carrotModelInStorage;
        [SerializeField] protected Transform _carrotSpawnPoint;

        protected override void OnCancelAction(Action action) { }

        protected List<CarrotModelInStorage> _carrotList = new ();
        public int CarrotCount =>
            _carrotList.Count;

        public void AddCarrot()
        {
            _carrotList.Add(Instantiate(_carrotModelInStorage, _carrotSpawnPoint.position, Quaternion.identity, transform));
            GameManager.CurrentGameInspector?.CarrotPicked(PlayerType.Mole);
        }

        public override bool CanInteract(Backpack backpack)
        {
            return backpack.Carrot.Count == 1;
        }

        protected override bool Action(Backpack backpack, Func<ActionType, float> OnActionRequested, Action OnActionCompleted)
        {
            /// we are sure that
            /// 1. Rabbit are not try to steal from underground
            /// 2. Mole have carrot in inventory and want to deposit it
            if (!backpack.Carrot.TryGet(1))
                return false;
            AddCarrot();

            var actionTime = OnActionRequested.Invoke(ActionType.PutDownCarrot);
            StartCoroutine(CompliteAction(OnActionCompleted, actionTime));
            return true;
        }
    }
}
using Extensions;
using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Base;
using RabbitVsMole.InteractableGameObject.Enums;
using RabbitVsMole.InteractableGameObject.Visuals;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitVsMole.InteractableGameObject.Storages
{
    public class FarmCarrotStorage : StorageBase
    {
        [Header("Visuals")]
        [SerializeField] protected CarrotModelInStorage _carrotModelInStorage;
        [SerializeField] protected Transform _carrotSpawnPoint;
        [SerializeField] ParticleSystem _particleForCarrotStealProgress;

        private bool _carrotStealProgress = false;
        private Coroutine _carrotStealCoroutine;

        protected List<CarrotModelInStorage> _carrotList = new ();
        public int CarrotCount =>
            _carrotList.Count;

        public bool CanStealCarrot =>
            CarrotCount > 0
            && !_carrotStealProgress;

        private void AddCarrot()
        {
            GameInspector.CarrotPicked(PlayerType.Rabbit);
            _carrotList.Add(Instantiate(_carrotModelInStorage, _carrotSpawnPoint.position, Quaternion.identity, transform));
        }


        private bool DeleteCarrot()
        {
            if (!CanStealCarrot) return false;

            var random_carrot = _carrotList.GetRandomElement();
            if(random_carrot.Delete())
                _carrotList.Remove(random_carrot);

            return true;
        }
        public bool StartStealCarrot()
        {
            if (!CanStealCarrot) return false;

            _carrotStealProgress = true;
            _particleForCarrotStealProgress.SafePlay();

            return true;
        }

        public bool EndStealCarrot()
        {
            if(!_carrotStealProgress) 
                return false;

            DeleteCarrot();
            _particleForCarrotStealProgress.SafeStop();
            GameInspector.CarrotStealed(PlayerType.Rabbit);

            return true;
        }
        public override bool CanInteract(Backpack backpack)
        {
            if(_carrotStealProgress)
                return false;

            return backpack.PlayerType switch
            {
                // put carrot
                PlayerType.Rabbit => backpack.Carrot.Count == 1,
                // steal carrot
                PlayerType.Mole => (backpack.Carrot.Count == 0 && CarrotCount > 0),
                _ => false,
            };
        }

        protected override bool Action(Backpack backpack, Func<ActionType, float> OnActionRequested, Action OnActionCompleted)
        {
            /// we are sure that
            /// 1. Rabbit can put carrou becouse have in inventory
            /// 2. Mole can steal becouse not hav in inventory and can steal something
            /// 2. Carrot Steal Progress is false
            return backpack.PlayerType switch
            {
                PlayerType.Rabbit => ActionForRabbit(backpack, OnActionRequested, OnActionCompleted),
                PlayerType.Mole => ActionForMole(backpack, OnActionRequested, OnActionCompleted),
                _ => false,
            };

        }

        private bool ActionForRabbit(Backpack backpack, Func<ActionType, float> OnActionRequested, Action OnActionCompleted)
        {
            if (!backpack.Carrot.TryGet())
                return false;

            AddCarrot();

            var actionTime = OnActionRequested.Invoke(ActionType.PutDownCarrot);
            StartCoroutine(CompliteAction(OnActionCompleted, actionTime));
            return true;
        }

        private bool ActionForMole(Backpack backpack, Func<ActionType, float> OnActionRequested, Action OnActionCompleted)
        {
            var actionTime = OnActionRequested.Invoke(ActionType.StealCarrotFromStorage);
            _carrotStealCoroutine =  StartCoroutine(CompliteActionForMole(backpack, OnActionCompleted, actionTime));
            return true;
        }

        protected override void OnCancelAction() {
            if (_carrotStealCoroutine == null)
                return;
            EndStealCarrot();
            StopCoroutine(_carrotStealCoroutine);
            _carrotStealCoroutine = null;
        }
       

        protected IEnumerator CompliteActionForMole(Backpack backpack, Action action, float time)
        {
            var currentTime = 0.0f;
            while (true)
            {
                
                currentTime += Time.deltaTime;
                if (currentTime >= time)
                {
                    _carrotStealCoroutine = null;
                    EndStealCarrot();

                    if(backpack.Carrot.TryInsert())
                        action?.Invoke();

                    yield break;
                }
                yield return null;
            }
        }
    }
}
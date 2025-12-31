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

        Coroutine _breakActionCorutine;
        Coroutine _rabbitActionCorutine;
        protected List<CarrotModelInStorage> _carrotList = new ();
        public int CarrotCount =>
            _carrotList.Count;

        public bool CanStealCarrot =>
            CarrotCount > 0
            && !_carrotStealProgress;

        private bool _carrotStealProgress = false;

        private void SpawnCarrotVisual() =>
            _carrotList.Add(Instantiate(_carrotModelInStorage, _carrotSpawnPoint.position, Quaternion.identity, transform));

        private void DeleteCarrotVisual()
        {
            var random_carrot = _carrotList.GetRandomElement();
            if (random_carrot == null)
                return;
            
            _carrotList.Remove(random_carrot);
            random_carrot.Delete();
        }

        public override bool CanInteract(Backpack backpack) =>
            backpack.PlayerType switch
            {
                // put carrot
                PlayerType.Rabbit => !_carrotStealProgress && backpack.Carrot.Count == 1,
                // steal carrot
                PlayerType.Mole => (CanStealCarrot && backpack.Carrot.Count == 0),
                _ => false,
            };

        protected override bool Action(Backpack backpack, Func<ActionType, float> OnActionRequested, Action OnActionCompleted) =>
            backpack.PlayerType switch
            {
                PlayerType.Rabbit => ActionForRabbit(backpack, OnActionRequested, OnActionCompleted),
                PlayerType.Mole => ActionForMole(backpack, OnActionRequested, OnActionCompleted),
                _ => false,
            };

        private bool ActionForRabbit(Backpack backpack, Func<ActionType, float> OnActionRequested, Action OnActionCompleted)
        {
            if (!backpack.Carrot.TryGet())
                return false;

            SpawnCarrotVisual();
            GameInspector.CarrotPicked(PlayerType.Rabbit);

            var actionTime = OnActionRequested.Invoke(ActionType.PutDownCarrot);
            _rabbitActionCorutine = StartCoroutine(CompliteAction(OnActionCompleted, actionTime));
            return true;
        }

        private bool ActionForMole(Backpack backpack, Func<ActionType, float> OnActionRequested, Action OnActionCompleted)
        {
            var actionTime = OnActionRequested.Invoke(ActionType.StealCarrotFromStorage);
            _breakActionCorutine = StartCoroutine(CompliteActionForMole(backpack, OnActionCompleted, actionTime));

            _particleForCarrotStealProgress.SafePlay();
            return true;
        }

        protected override void OnCancelAction(Action OnActionCompleted) {
            if (_breakActionCorutine != null)
            {
                StopCoroutine(_breakActionCorutine);
                _breakActionCorutine = null;
            }
            if (_rabbitActionCorutine != null)
            {
                StopCoroutine(_rabbitActionCorutine);
                _rabbitActionCorutine = null;
            }
            OnActionCompleted?.Invoke();
            _particleForCarrotStealProgress.SafeStop();
        }
       

        protected IEnumerator CompliteActionForMole(Backpack backpack, Action action, float time)
        {
            var currentTime = 0.0f;
            while (true)
            {
                currentTime += Time.deltaTime;
                if (currentTime >= time)
                {
                    if (backpack.Carrot.TryInsert())
                    {
                        GameInspector.CarrotStealed(PlayerType.Rabbit);
                        _particleForCarrotStealProgress.SafeStop();
                        DeleteCarrotVisual();
                        action?.Invoke();
                    }

                    yield break;
                }
                yield return null;
            }
        }
    }
}
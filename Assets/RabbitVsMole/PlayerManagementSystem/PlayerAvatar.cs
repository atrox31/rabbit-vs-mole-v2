using Extensions;
using GameSystems;
using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.Events;
using RabbitVsMole.InteractableGameObject.Base;
using RabbitVsMole.InteractableGameObject.Enums;
using RabbitVsMole.InteractableGameObject.Field;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WalkingImmersionSystem;

namespace RabbitVsMole
{
    /// <summary>
    /// PlayerAvatar - handles movement, animations and interactions for player character.
    /// Can be controlled by PlayerController (human input), AI, or network input.
    /// </summary>
    public class PlayerAvatar : PlayerManagementSystem.PlayerAvatarBase
    {
        [Header("Player Type")]
        public PlayerType playerType;

        [Header("Addons")]
        [SerializeField] private List<AvatarAddon> _avatarAddonList;

        [Header("Movement Settings")]
        AvatarStats avatarStats;
        [SerializeField] private float _baseWalkSpeed = 5f;

        [Header("Interaction Settings")]
        [SerializeField] private float _raycastDistance = 3f;
        [SerializeField] private LayerMask interactionLayerMask = -1;
        [SerializeField] private ParticleSystem _hitParticles;

        private Rigidbody _rigidbody;
        private SpeedController _speedController;
        private Animator _animator;
        public Backpack Backpack { get; private set; }
        public bool IsHaveCarrot => Backpack.Carrot.Count == 1;

        // Movement input (set externally by controller)
        private Vector2 _moveInput;

        // Action States
        private bool _isPerformingAction;

        private IInteractableGameObject _interactableOnFront;
        private IInteractableGameObject _interactableDown;
        private IInteractableGameObject _interactableNearby;
        private PlayerAvatar _enemy;
        public PlayerAvatar EnemyInRange => _enemy;
        public bool EnemyIsInRange => _enemy != null;
        private Action _currentCancelAction;

        public bool IsInteractionAvableFront => _interactableOnFront != null;
        public bool IsInteractionAvableDown => _interactableDown != null;
        public bool IsEnemyInRange => _enemy != null;

        public IInteractableGameObject NearbyInteractable => _interactableNearby;

        // Animation Parameters
        private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
        private static readonly int StartWalkHash = Animator.StringToHash("StartWalk");
        private static readonly int StopWalkHash = Animator.StringToHash("StopWalk");

        public PlayerType PlayerType => playerType;
        public bool IsPerformingAction => _isPerformingAction;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _animator = GetComponentInChildren<Animator>();

            if (_rigidbody == null)
            {
                enabled = false;
                return;
            }
            avatarStats = new AvatarStats(_baseWalkSpeed);
            _speedController = new SpeedController(avatarStats);
            Backpack = new Backpack(playerType);

            foreach (var item in _avatarAddonList)
            {
                item.Setup(this);
            }
        }

        private void ShowAddon(ActionType actionType)
        {
            foreach (var item in _avatarAddonList)
            {
                item.Hide();
                if (item.actionType == actionType)
                {
                    item.Show();
                }
            }
        }

        private void Update()
        {
            UpdateAnimations();
            ScanForInteractions();
        }

        private void FixedUpdate()
        {
            HandleMovement();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.isTrigger) return;
            if (other.gameObject.TryGetComponent(out PlayerAvatar avatar))
            {
                if (avatar.playerType != playerType)
                {
                    _enemy = avatar;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.isTrigger) return;
            if (_enemy != null
                && other.gameObject.TryGetComponent(out PlayerAvatar avatar))
            {
                if (avatar.playerType != playerType)
                {
                    _enemy = null;
                }
            }
        }

        private void ScanForInteractions()
        {
            var currentFront = FindInteractableWithRaycast(transform.forward, Color.yellow);
            var currentDown = FindInteractableWithRaycast(Vector3.down, Color.red);

            if (_interactableOnFront != currentFront)
            {
                _interactableOnFront?.LightDown();
                _interactableOnFront = currentFront;
                _interactableOnFront?.LightUp();
            }

            if (_interactableDown != currentDown)
            {
                _interactableDown?.LightDown();
                _interactableDown = currentDown;
                _interactableDown?.LightUp();
            }
        }

        /// <summary>
        /// Sets the movement input from controller (human, AI, or network)
        /// </summary>
        public void SetMoveInput(Vector2 moveInput)
        {
            if (_isPerformingAction)
                return;

            _moveInput = moveInput;
        }

        /// <summary>
        /// Returns the forward direction as a Vector2 (x, z from transform.forward)
        /// </summary>
        public Vector2 GetForwardVector2()
        {
            Vector3 forward = transform.forward;
            return new Vector2(forward.x, forward.z);
        }

        /// <summary>
        /// Tries to perform front action (interaction in front direction)
        /// </summary>
        public bool TryActionFront()
        {
            if (_isPerformingAction)
                return false;
            return PerformInteraction(_interactableOnFront);
        }

        /// <summary>
        /// Tries to perform down action (interaction in down direction)
        /// </summary>
        public bool TryActionDown()
        {
            if (_isPerformingAction)
                return false;
            return PerformInteraction(_interactableDown);
        }

        /// <summary>
        /// Tries to perform special action
        /// </summary>
        public bool TryActionSpecial()
        {
            if (_isPerformingAction)
                return false;

            if (EnemyIsInRange)
            {
                StartCoroutine(AttackCoorutine());
                return true;
            }
            return false;
        }

        IEnumerator AttackCoorutine()
        {
            _isPerformingAction = true;
            TriggerActionAnimation(ActionType.CollapseMound, 2f);
            _enemy.Hit();
            yield return new WaitForSeconds(2);
            _isPerformingAction = false;
        }

        private void Hit()
        {
            _hitParticles.SafePlay();
        }

        private void HandleMovement()
        {
            if (_rigidbody == null || _speedController == null)
                return;

            bool isMoving = _moveInput.sqrMagnitude > 0.01f;
            _speedController.HandleAcceleration(isMoving);

            if (isMoving && _speedController.Current > _speedController.SpeedMargin)
            {
                Vector3 moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
                Vector3 targetVelocity = moveDirection * _speedController.Current;
                Vector3 currentVelocity = _rigidbody.linearVelocity;
                _rigidbody.linearVelocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);

                if (moveDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, avatarStats.RotationSpeed * Time.fixedDeltaTime);
                }
            }
            else
            {
                Vector3 currentVelocity = _rigidbody.linearVelocity;
                _rigidbody.linearVelocity = new Vector3(0f, currentVelocity.y, 0f);
            }
        }

        private bool PerformInteraction(IInteractableGameObject interactable)
        {
            if (_isPerformingAction || interactable is null)
                return false;

            if (IsHaveCarrot && interactable is not StorageBase)
                return false;

            _isPerformingAction = interactable.Interact(
                this,
                OnActionRequested,
                OnActionCompleted,
                out _currentCancelAction);

            return _isPerformingAction;
        }

        private float OnActionRequested(ActionType requestedAction)
        {
            ShowAddon(requestedAction);
            var actionTIme = GetActionTime(requestedAction);
            TriggerActionAnimation(requestedAction, actionTIme);
            return actionTIme;
        }

        private void OnActionCompleted()
        {
            _isPerformingAction = false;
        }

        private float GetActionTime(ActionType actionType) =>
            actionType switch
            {
                ActionType.PlantSeed => GameInspector.GameStats.TimeActionPlantSeed,
                ActionType.WaterField => GameInspector.GameStats.TimeActionWaterField,
                ActionType.HarvestCarrot => GameInspector.GameStats.TimeActionHarvestCarrot,
                ActionType.RemoveRoots => GameInspector.GameStats.TimeActionRemoveRoots,
                ActionType.StealCarrotFromUndergroundField => GameInspector.GameStats.TimeActionStealCarrotFromUndergroundField,
                ActionType.DigUndergroundWall => GameInspector.GameStats.TimeActionDigUndergroundWall,
                ActionType.DigMound => GameInspector.GameStats.TimeActionDigMound,
                ActionType.CollapseMound => GameInspector.GameStats.TimeActionCollapseMound,
                ActionType.EnterMound => GameInspector.GameStats.TimeActionEnterMound,
                ActionType.PickSeed => GameInspector.GameStats.TimeActionPickSeed,
                ActionType.PickWater => GameInspector.GameStats.TimeActionPickWater,
                ActionType.PutDownCarrot => GameInspector.GameStats.TimeActionPutDownCarrot,
                ActionType.StealCarrotFromStorage => GameInspector.GameStats.TimeActionStealCarrotFromStorage,
                ActionType.None => 0f,
                _ => 0f
            };
        
        private void TriggerActionAnimation(ActionType actionType, float actionTime)
        {
            if (_animator == null)
                return;

            string triggerName = GetAnimationTriggerName(actionType);
            if (!string.IsNullOrEmpty(triggerName))
            {
                _animator.SetTrigger(triggerName);
            }
        }

        private IInteractableGameObject FindInteractableWithRaycast(Vector3 direction, Color color)
        {
            Vector3 rayOrigin = transform.position;
            Debug.DrawRay(rayOrigin, direction * _raycastDistance, color);

            if (Physics.Raycast(rayOrigin, direction, out RaycastHit hit, _raycastDistance, interactionLayerMask, QueryTriggerInteraction.Collide))
            {
                if (hit.collider.TryGetComponent<IInteractableGameObject>(out var interactable))
                {
                    return interactable;
                }
            }

            return null;
        }

        private bool IsAnyInteractableNearby()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, _raycastDistance, interactionLayerMask);
            foreach (var collider in colliders)
            {
                if (collider.TryGetComponent<IInteractableGameObject>(out _interactableNearby))
                {
                    return true;
                }
            }

            return false;
        }


        private string GetAnimationTriggerName(ActionType actionType) => 
            $"{playerType}_{actionType}";

        private void UpdateAnimations()
        {
            if (_animator == null)
                return;

            bool isMoving = _moveInput.sqrMagnitude > 0.01f && _speedController != null && _speedController.Current > _speedController.SpeedMargin;
            bool wasWalking = _animator.GetBool(IsWalkingHash);

            if (isMoving != wasWalking)
            {
                _animator.SetBool(IsWalkingHash, isMoving);
                if (isMoving)
                {
                    _animator.SetTrigger(StartWalkHash);
                }
                else
                {
                    _animator.SetTrigger(StopWalkHash);
                }
            }

        }

        public void MoveToLinkedField(InteractableGameObject.Base.FieldBase linkedField) =>
            StartCoroutine(MoveToLinkedFieldInternal(linkedField));

        private IEnumerator MoveToLinkedFieldInternal(FieldBase linkedField)
        {
            var moveInActionTime = GetActionTime(ActionType.EnterMound);
            var moveOutActionTime = GetActionTime(ActionType.ExitMound);
            var newLocation = linkedField.gameObject.transform.position;
            EventBus.Publish(new MoleTravelEvent() { EnterTime = moveInActionTime, ExitTime = moveOutActionTime, NewLocation = newLocation });

            float moveInElapsedTime = 0f;
            while (moveInElapsedTime < moveInActionTime)
            {
                moveInElapsedTime += Time.deltaTime;
                yield return null;
            }

            yield return null;
            SetupNewTerrain();
        }

        private void SetupNewTerrain()
        {
            var walkingImmersion = GetComponentInChildren<WalkingImmersionSystemController>();
            if (walkingImmersion == null)
            {
                DebugHelper.LogError(this, "WalkingImmersionSystemController not found");
                return;
            }
            if (!walkingImmersion.SetupTerrain())
                return;
        }
    }
}
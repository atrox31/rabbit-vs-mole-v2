using Extensions;
using GameSystems;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.Events;
using RabbitVsMole.InteractableGameObject.Base;
using RabbitVsMole.InteractableGameObject.Enums;
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
        [SerializeField] private List<AvatarAddon> _avatarAddonListPrefab;
        private List<AvatarAddon> _activeAvatarAddon = new();

        [Header("Interaction Settings")]
        [SerializeField] private float _raycastDistance = 1f;
        [SerializeField] private LayerMask interactionLayerMask = -1;
        [SerializeField] private ParticleSystem _hitParticles;

        private Rigidbody _rigidbody;
        private SpeedController _speedController;
        private Animator _animator;
        public bool IsOnSurface =>
            transform.position.y > 0;

        public Backpack Backpack { get; private set; }
        public bool IsHaveCarrot => Backpack.Carrot.Count == 1;
        [SerializeField] private GameObject _haveCarrotIndicator;

        // Movement input (set externally by controller)
        private Vector2 _moveInput;

        // Action States
        private bool _isPerformingAction;
        private float _defaultAnimatorSpeed = 1f;
        private Coroutine _animationSpeedCoroutine;

        private IInteractableGameObject _interactableOnFront;
        private IInteractableGameObject _interactableDown;
        private IInteractableGameObject _interactableNearby;
        private PlayerAvatar _enemy;
        public PlayerAvatar EnemyInRange => _enemy;
        public bool EnemyIsInRange => _enemy != null;
        private Action<Action> _currentCancelAction;

        public bool IsInteractionAvableFront => _interactableOnFront != null;
        public bool IsInteractionAvableDown => _interactableDown != null;
        public bool IsEnemyInRange => _enemy != null;
        private Vector3 _respawnPosition;
        private Coroutine _healthRegeneration;

        public IInteractableGameObject NearbyInteractable => _interactableNearby;

        // Animation Parameters
        private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
        private Dictionary<string, float> _animationClipLengths = new Dictionary<string, float>();
        private AnimationState _currentAnimationState = AnimationState.None;

        private enum AnimationState
        {
            None,
            Idle,
            Walk,
            Action
        }

        public PlayerType PlayerType => playerType;
        public bool IsPerformingAction => _isPerformingAction;

        public static PlayerAvatar MoleStaticInstance { get; private set; }
        public static PlayerAvatar RabbitStaticInstance { get; private set; }
      
        private void Awake()
        {
            switch (playerType)
            {
                case PlayerType.Rabbit:
                    RabbitStaticInstance = this;
                    break;
                case PlayerType.Mole:
                    MoleStaticInstance = this;
                    break;
            }

            _rigidbody = GetComponent<Rigidbody>();
            _animator = GetComponentInChildren<Animator>();

            if (_rigidbody == null)
            {
                enabled = false;
                return;
            }
            _speedController = new SpeedController(playerType);
            Backpack = new Backpack(playerType);

            if (_animator != null)
            {
                _defaultAnimatorSpeed = _animator.speed;
                foreach (var clip in _animator.runtimeAnimatorController.animationClips)
                {
                    if (!_animationClipLengths.ContainsKey(clip.name))
                    {
                        _animationClipLengths.Add(clip.name, clip.length);
                    }
                }
            }

            _haveCarrotIndicator.SetActive(false);

            foreach (var item in _avatarAddonListPrefab)
            {
                var activeAddon = Instantiate(item);
                activeAddon.Setup(this);
                _activeAvatarAddon.Add(activeAddon);
            }
        }

        private void OnDestroy()
        {
            switch (playerType)
            {
                case PlayerType.Rabbit:
                    RabbitStaticInstance = null;
                    break;
                case PlayerType.Mole:
                    MoleStaticInstance = null;
                    break;
            }
        }

        private void Start()
        {
            _respawnPosition = transform.position;
        }

        private void ShowAddon(ActionType actionType)
        {
            foreach (var item in _activeAvatarAddon)
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
                _interactableOnFront?.LightDown(playerType);
                _interactableOnFront = currentFront;
                _interactableOnFront?.LightUp(playerType);
            }

            if (_interactableDown != currentDown)
            {
                _interactableDown?.LightDown(playerType);
                _interactableDown = currentDown;
                _interactableDown?.LightUp(playerType);
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

            if (EnemyIsInRange && GameInspector.CurrentGameMode.AllowFight)
            {
                return PerformAction(
                        actionType: ActionType.Attack,
                        onBegin: () => _enemy.Hit(GameInspector.GameStats.FightRabbitDamageDeal),
                        onEnd: null,
                        blockAfterAction: false
                    );
            }
            return false;
        }

        public bool PerformAction(
            ActionType actionType,
            Action onBegin = null,
            Action onEnd = null,
            bool blockAfterAction = false
            )
        {
            if(_actionCorutine != null)
            {
                StopCoroutine(_actionCorutine);
                _currentCancelAction?.Invoke(OnActionCompleted);
            }
            _actionCorutine = StartCoroutine(ActionCoroutine(actionType, onBegin, onEnd, blockAfterAction));
            return true;
        }
        Coroutine _actionCorutine;

        IEnumerator ActionCoroutine(
            ActionType actionType,
            Action onBegin,
            Action onEnd,
            bool blockAfterAction)
        {
            _isPerformingAction = true;
            _moveInput = Vector3.zero;
            onBegin?.Invoke();

            yield return new WaitForSeconds(OnActionRequested(actionType));

            onEnd?.Invoke();

            if(!blockAfterAction)
                OnActionCompleted();

            _actionCorutine = null;
        }

        private bool Hit(int damage)
        {
            // cancel current action
            _currentCancelAction?.Invoke(null);
            // drop carrot
            Backpack.Carrot.TryGet();

            if (Backpack.Health.TryGet(damage))
            {
                // hit but have some health
                return PerformAction(
                        actionType: ActionType.Stun,
                        onBegin: () => _hitParticles.SafePlay(),
                        onEnd: () => _hitParticles.SafeStop(),
                        blockAfterAction: false
                    );
            }
            else
            {
                // healts is too low
                Backpack.Health.GetAll();
                return PerformAction(
                        actionType: ActionType.Death,
                        onBegin: () => _hitParticles.SafePlay(),
                        onEnd: () =>
                        {
                            EventBus.Publish(new TravelEvent() { NewLocation = _respawnPosition, actionTypeAfterTravel = ActionType.Respawn });
                            _healthRegeneration ??= StartCoroutine(HealthRegenerationCoroutine());
                        },
                        blockAfterAction: true
                    ); 
            }
        }

        public void MoundCollapse(FieldBase field)
        {
            if (_interactableDown == null)
                return;

            if(field is not UndergroundFieldBase collapsedUndergroundField)
                return;

            if(_interactableDown is not UndergroundFieldBase fieldThatPlayerIsStandingOn)
                return;

            if(collapsedUndergroundField == fieldThatPlayerIsStandingOn)
                Hit(GameInspector.GameStats.FightMoleHealthPoints + 1);
        }

        IEnumerator HealthRegenerationCoroutine()
        {
            while (!Backpack.Health.IsFull)
            {
                bool canRegenerate = !IsOnSurface || GameInspector.GameStats.FightMoleAllowRegenerationOnSurface;

                if (canRegenerate)
                    Backpack.Health.TryInsert(GameInspector.GameStats.FightMoleHealthRegenerationPerSec, true);

                yield return new WaitForSeconds(1f);
            }
            _healthRegeneration = null;
        }

        private void HandleMovement()
        {
            if (_rigidbody == null || _speedController == null)
                return;

            if(_isPerformingAction)
                _moveInput = Vector2.zero;

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
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _speedController.RotationSpeed * Time.fixedDeltaTime);
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
            _currentAnimationState = AnimationState.None; // Reset state to allow UpdateAnimations to take over
            ShowAddon(ActionType.None);
            _haveCarrotIndicator.SetActive(IsHaveCarrot);
            _currentCancelAction = null;
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
                ActionType.DigMound => IsOnSurface
                    ? GameInspector.GameStats.TimeActionDigMoundOnSurface
                    : GameInspector.GameStats.TimeActionDigMoundUnderground,
                ActionType.CollapseMound => GameInspector.GameStats.TimeActionCollapseMound,
                ActionType.EnterMound => GameInspector.GameStats.TimeActionEnterMound,
                ActionType.ExitMound => GameInspector.GameStats.TimeActionExitMound,
                ActionType.PickSeed => GameInspector.GameStats.TimeActionPickSeed,
                ActionType.PickWater => GameInspector.GameStats.TimeActionPickWater,
                ActionType.PutDownCarrot => GameInspector.GameStats.TimeActionPutDownCarrot,
                ActionType.StealCarrotFromStorage => GameInspector.GameStats.TimeActionStealCarrotFromStorage,
                ActionType.Attack => GameInspector.GameStats.FightRabbitAttackActionTime,
                ActionType.Stun => GameInspector.GameStats.FightMoleStunTime,
                ActionType.Respawn => GameInspector.GameStats.FightMoleRespawnTime,
                ActionType.Death => GameInspector.GameStats.FightMoleDeath,
                ActionType.None => 0f,
                ActionType.Victory => float.MaxValue,
                ActionType.Defeat => float.MaxValue,
                _ => 0f
            };
        
        private void TriggerActionAnimation(ActionType actionType, float actionTime)
        {
            if (_animator == null)
                return;

            _moveInput = Vector2.zero;
            ResetNavTriggers(); // Ensure no navigation triggers are pending
            string triggerName = GetAnimationTriggerName(actionType);

            if (_animationClipLengths.TryGetValue(triggerName, out float clipLength))
            {
                if (actionTime > 0f && clipLength > 0f)
                {
                    _animator.speed = clipLength / actionTime;
                }
                else
                {
                    _animator.speed = _defaultAnimatorSpeed;
                }
            }
            else
            {
                DebugHelper.LogWarning(this, $"Animation clip '{triggerName}' not found in cache. Using default speed.");
                _animator.speed = _defaultAnimatorSpeed;
            }

            _animator.SetTrigger(triggerName);
            _currentAnimationState = AnimationState.Action;
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
            if (_animator == null) return;

            // If performing an action, we do nothing - TriggerActionAnimation handles logical state
            // But we must ensure specific states if NOT acting
            if (_isPerformingAction) return;

            bool isMoving = _moveInput.sqrMagnitude > 0.01f && _speedController != null && _speedController.Current > _speedController.SpeedMargin;
            _animator.SetBool(IsWalkingHash, isMoving); // Restore Bool for controllers that rely on it

            if (isMoving)
            {
                if (_currentAnimationState != AnimationState.Walk)
                {
                    ResetNavTriggers();
                    _animator.speed = _defaultAnimatorSpeed;
                    _animator.SetTrigger("StartWalk");
                    _currentAnimationState = AnimationState.Walk;
                }
            }
            else
            {
                if (_currentAnimationState == AnimationState.Walk)
                {
                    ResetNavTriggers();
                    _animator.speed = _defaultAnimatorSpeed;
                    _animator.SetTrigger("StopWalk");
                    _currentAnimationState = AnimationState.Idle; 
                }

                if (_currentAnimationState != AnimationState.Idle)
                {
                    ResetNavTriggers();
                    _animator.speed = _defaultAnimatorSpeed;
                    string idleTrigger = $"{playerType}_Idle";
                    _animator.SetTrigger(idleTrigger);
                    _currentAnimationState = AnimationState.Idle;
                }
            }
        }
        
        private void ResetNavTriggers()
        {
            _animator.ResetTrigger("StartWalk");
            _animator.ResetTrigger("StopWalk");
            _animator.ResetTrigger($"{playerType}_Idle");
        }

        public void MoveToLinkedField(InteractableGameObject.Base.FieldBase linkedField) =>
            StartCoroutine(MoveToLinkedFieldInternal(linkedField));

        private IEnumerator MoveToLinkedFieldInternal(FieldBase linkedField)
        {
            var moveInActionTime = GetActionTime(ActionType.EnterMound);
            var moveOutActionTime = GetActionTime(ActionType.ExitMound);

            // Start raycast from above the field position to find ground surface
            var fieldPosition = linkedField.gameObject.transform.position;
            var rayStartPosition = fieldPosition + Vector3.up * 10f; 
            var raycastDistance = 100f;
            
            var newLocation = fieldPosition; // Default fallback
            
            // Cast ray downward to find ground surface
            if (Physics.Raycast(rayStartPosition, Vector3.down, out RaycastHit hit, raycastDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                newLocation = hit.point;
            }

            EventBus.Publish(new TravelEvent() { NewLocation = newLocation, actionTypeAfterTravel = ActionType.ExitMound });

            float moveInElapsedTime = 0f;
            while (moveInElapsedTime < moveInActionTime)
            {
                moveInElapsedTime += Time.deltaTime;
                yield return null;
            }

            yield return null;
            SetupNewTerrain();
        }

        public void SetupNewTerrain()
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
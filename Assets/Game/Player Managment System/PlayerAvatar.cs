using UnityEngine;
using GameObjects.Misc;
using Enums;
using System;

/// <summary>
/// PlayerAvatar - handles movement, animations and interactions for player character.
/// Can be controlled by PlayerController (human input), AI, or network input.
/// </summary>
public class PlayerAvatar : MonoBehaviour
{
    [Header("Player Type")]
    public PlayerType playerType;

    [Header("Movement Settings")]
    AvatarStats avatarStats;
    [SerializeField] private float _baseWalkSpeed = 5f;

    [Header("Interaction Settings")]
    [SerializeField] private float _raycastDistance = 3f;
    [SerializeField] private LayerMask interactionLayerMask = -1;

    private Rigidbody _rigidbody;
    private SpeedController _speedController;
    private Animator _animator;
    
    // Movement input (set externally by controller)
    private Vector2 _moveInput;
    
    // Action States
    private bool _isPerformingAction;

    private IInteractable _interactableOnFront;
    private IInteractable _interactableDown;

    public bool IsInteractionAvableFront => _interactableOnFront != null;
    public bool IsInteractionAvableDown => _interactableDown != null;

    // Animation Parameters
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int StartWalkHash = Animator.StringToHash("StartWalk");
    private static readonly int StopWalkHash = Animator.StringToHash("StopWalk");
    private static readonly int BlendHash = Animator.StringToHash("Blend");

    public PlayerType GetPlayerType() => playerType;
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

    private void ScanForInteractions()
    {
        if(IsAnyInteractableNearby() == false)
        {
            _interactableOnFront = null;
            _interactableDown = null;
            return;
        }
        _interactableOnFront = FindInteractableWithRaycast(Vector3.down);
        _interactableDown = FindInteractableWithRaycast(transform.forward);
    }

    /// <summary>
    /// Sets the movement input from controller (human, AI, or network)
    /// </summary>
    public void SetMoveInput(Vector2 moveInput)
    {
        _moveInput = moveInput;
    }

    /// <summary>
    /// Tries to perform front action (interaction in front direction)
    /// </summary>
    public void TryActionFront()
    {
        if (_isPerformingAction)
            return;
        PerformInteraction(_interactableOnFront);
    }

    /// <summary>
    /// Tries to perform down action (interaction in down direction)
    /// </summary>
    public void TryActionDown()
    {
        if (_isPerformingAction)
            return;
        PerformInteraction(_interactableDown);
    }

    /// <summary>
    /// Tries to perform special action
    /// </summary>
    public void TryActionSpecial()
    {
        if (_isPerformingAction)
            return;

        // Special action doesn't require raycast - find nearest interactable
        // IInteractable interactable = FindNearestInteractable();
        // TODO: custom actions
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

    private void PerformInteraction(IInteractable interactable)
    {
        if(_isPerformingAction || interactable is null)
            return;

        _isPerformingAction = true;
        interactable.Interact(playerType, OnActionRequested, OnActionCompleted);
    }

    private bool OnActionRequested(ActionType requestedAction)
    {
        // Trigger animation for the requested action
        TriggerActionAnimation(requestedAction);
        return true;
    }

    private void OnActionCompleted(bool success)
    {
        _isPerformingAction = false;
    }

    private IInteractable FindInteractableWithRaycast(Vector3 direction)
    {
        Vector3 rayOrigin = transform.position;
        
        // Adjust ray origin slightly above ground for forward raycast
        if (direction == transform.forward)
        {
            rayOrigin += Vector3.up * 0.5f;
        }

        if (Physics.Raycast(rayOrigin, direction, out RaycastHit hit, _raycastDistance, interactionLayerMask))
        {
            return hit.collider.GetComponent<IInteractable>();
        }

        return null;
    }

    private bool IsAnyInteractableNearby()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _raycastDistance, interactionLayerMask);
        foreach (var collider in colliders)
        {
            if(collider.TryGetComponent<IInteractable>(out _))
            {
                return true;
            }
        }

        return false;
    }

    private void TriggerActionAnimation(ActionType actionType)
    {
        if (_animator == null)
            return;

        string triggerName = GetAnimationTriggerName(actionType);
        if (!string.IsNullOrEmpty(triggerName))
        {
            _animator.SetTrigger(triggerName);
        }
    }

    private string GetAnimationTriggerName(ActionType actionType)
    {
        if (playerType == PlayerType.Rabbit)
        {
            return actionType switch
            {
                ActionType.Plant => "Rabbit_Plant",
                ActionType.Water => "Rabbit_Water",
                ActionType.Harvest => "Rabbit_Harvest",
                ActionType.RemoveRoots => "Rabbit_RemoveRoots",
                ActionType.CollapseMound => "Rabbit_CollapseMound",
                _ => string.Empty
            };
        }
        else
        {
            return actionType switch
            {
                ActionType.DigMound => "Mole_DigMound",
                ActionType.CollapseMound => "Mole_CollapseMound",
                ActionType.RemoveRoots => "Mole_RemoveRoots",
                _ => string.Empty
            };
        }
    }

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

        if (isMoving)
        {
            float speedNormalized = _speedController != null ? _speedController.Current / avatarStats.MaxWalkSpeed : 0f;
            _animator.SetFloat(BlendHash, speedNormalized);
        }
    }
}


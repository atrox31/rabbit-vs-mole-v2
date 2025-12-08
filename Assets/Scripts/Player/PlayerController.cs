using UnityEngine;
using UnityEngine.InputSystem;
using GameObjects.Misc;
using Enums;

/// <summary>
/// PlayerController - handles input system, movement, actions and animations for player.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Player Type")]
    [SerializeField] private PlayerType playerType;

    [Header("Movement Settings")]
    [SerializeField] private float _maxWalkSpeed = 5f;
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _deceleration = 20f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private float raycastDistance = 3f;
    [SerializeField] private LayerMask interactionLayerMask = -1;

    private PlayerInput _playerInput;
    private InputActionMap _playerActionMap;
    private Rigidbody _rigidbody;
    private SpeedController _speedController;
    private Animator _animator;
    
    // Input Actions
    private InputAction _moveAction;
    private InputAction _actionFrontAction;
    private InputAction _actionDownAction;
    private InputAction _actionSpecialAction;

    // Input Values
    public Vector2 MoveInput { get; private set; }
    public bool ActionFrontPressed { get; private set; }
    public bool ActionDownPressed { get; private set; }
    public bool ActionSpecialPressed { get; private set; }

    // Action States
    private bool _isPerformingAction;

    // Animation Parameters
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int StartWalkHash = Animator.StringToHash("StartWalk");
    private static readonly int StopWalkHash = Animator.StringToHash("StopWalk");
    private static readonly int BlendHash = Animator.StringToHash("Blend");

    public PlayerType GetPlayerType() => playerType;

    private void Awake()
    {
        _playerInput = GetComponentInParent<PlayerInput>();
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponentInChildren<Animator>();
        
        if (_rigidbody == null)
        {
            enabled = false;
            return;
        }

        _speedController = new SpeedController(_acceleration, _deceleration, _maxWalkSpeed);
    }

    private void Start()
    {
        InitializeInputActions();
    }

    private void OnEnable()
    {
        if (_playerInput != null)
        {
            InitializeInputActions();
        }
    }

    private void OnDisable()
    {
        DisableInputActions();
    }

    private void Update()
    {
        ReadInputValues();
        HandleActions();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void InitializeInputActions()
    {
        if (_playerInput == null || _playerInput.actions == null)
            return;

        if (string.IsNullOrEmpty(_playerInput.defaultActionMap))
        {
            _playerInput.defaultActionMap = "Player";
        }

        _playerActionMap = _playerInput.actions.FindActionMap("Player");
        if (_playerActionMap == null)
            return;

        _playerActionMap.Enable();

        _moveAction = _playerActionMap.FindAction("Move");
        _actionFrontAction = _playerActionMap.FindAction("Action_front");
        _actionDownAction = _playerActionMap.FindAction("Action_down");
        _actionSpecialAction = _playerActionMap.FindAction("Action_special");
        
        string currentControlScheme = _playerInput.currentControlScheme;
        if (!string.IsNullOrEmpty(currentControlScheme))
        {
            foreach (var action in _playerActionMap.actions)
            {
                action.bindingMask = InputBinding.MaskByGroup(currentControlScheme);
            }
        }

        _moveAction?.Enable();
        _actionFrontAction?.Enable();
        _actionDownAction?.Enable();
        _actionSpecialAction?.Enable();
    }

    private void DisableInputActions()
    {
        _playerActionMap?.Disable();
        MoveInput = Vector2.zero;
        ActionFrontPressed = false;
        ActionDownPressed = false;
        ActionSpecialPressed = false;
    }

    private void ReadInputValues()
    {
        MoveInput = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        ActionFrontPressed = _actionFrontAction?.IsPressed() ?? false;
        ActionDownPressed = _actionDownAction?.IsPressed() ?? false;
        ActionSpecialPressed = _actionSpecialAction?.IsPressed() ?? false;
    }

    private void HandleMovement()
    {
        if (_rigidbody == null || _speedController == null)
            return;

        bool isMoving = MoveInput.sqrMagnitude > 0.01f;
        _speedController.HandleAcceleration(isMoving);

        if (isMoving && _speedController.Current > _speedController.SpeedMargin)
        {
            Vector3 moveDirection = new Vector3(MoveInput.x, 0f, MoveInput.y).normalized;
            Vector3 targetVelocity = moveDirection * _speedController.Current;
            Vector3 currentVelocity = _rigidbody.linearVelocity;
            _rigidbody.linearVelocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);

            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }
        else
        {
            Vector3 currentVelocity = _rigidbody.linearVelocity;
            _rigidbody.linearVelocity = new Vector3(0f, currentVelocity.y, 0f);
        }
    }

    private void HandleActions()
    {
        if (_isPerformingAction)
            return;

        IInteractable interactable = null;

        if (ActionFrontPressed)
        {
            interactable = FindInteractableWithRaycast(transform.forward);
        }
        else if (ActionDownPressed)
        {
            interactable = FindInteractableWithRaycast(Vector3.down);
        }
        else if (ActionSpecialPressed)
        {
            // Special action doesn't require raycast - find nearest interactable
            //interactable = FindNearestInteractable();
            //TODO: custom actions
            return;
        }

        if (interactable != null)
        {
            PerformInteraction(interactable);
        }
    }

    private void PerformInteraction(IInteractable interactable)
    {
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
        RaycastHit hit;
        Vector3 rayOrigin = transform.position;
        
        // Adjust ray origin slightly above ground for forward raycast
        if (direction == transform.forward)
        {
            rayOrigin += Vector3.up * 0.5f;
        }

        if (Physics.Raycast(rayOrigin, direction, out hit, raycastDistance, interactionLayerMask))
        {
            return hit.collider.GetComponent<IInteractable>();
        }

        return null;
    }

    private IInteractable FindNearestInteractable()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRange, interactionLayerMask);
        float nearestDistance = float.MaxValue;
        IInteractable nearestInteractable = null;

        foreach (var collider in colliders)
        {
            var interactable = collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestInteractable = interactable;
                }
            }
        }

        return nearestInteractable;
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

        bool isMoving = MoveInput.sqrMagnitude > 0.01f && _speedController != null && _speedController.Current > _speedController.SpeedMargin;
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
            float speedNormalized = _speedController != null ? _speedController.Current / _maxWalkSpeed : 0f;
            _animator.SetFloat(BlendHash, speedNormalized);
        }
    }
}

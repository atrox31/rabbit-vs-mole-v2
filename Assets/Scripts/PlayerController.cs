using System;
using System.Collections.Generic;
using System.Linq;
using Enums;
using Extensions;
using GameObjects.Misc;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private const string ActionMap = "Player";
    private const float RayDistance = 1.5f;
    [SerializeField] private PlayerType playerType;

    [Header("Movement")]
    [SerializeField] private float maxWalkSpeed = 5f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 20f;
    private SpeedController _speedController;

    [SerializeField] private float rotationSpeed = 2.5f;
    private bool _isWalking;
    private Vector3 _currentDirection = Vector3.zero;
    
    [Header("Animation")]
    [SerializeField] private float animationSpeed = 1.8f;

    [Header("Pointers")]
    [SerializeField] private InputActionAsset inputActions;

    private Animator _animator;

    private InputAction _moveAction;
    private InputAction _interactAction;
    private Rigidbody _rigidbody;

    private readonly int _startWalkHash = Animator.StringToHash("StartWalk");
    private readonly int _stopWalkiHash = Animator.StringToHash("StopWalk");
    private readonly int _isWalkingHash = Animator.StringToHash("IsWalking");

    private Dictionary<ActionType, int> _animationHashes;

    private bool IsBusy { get; set; }

    private void OnEnable()
    {
        inputActions.FindActionMap(ActionMap).Enable();
        AudioManager.AudioLisnerRegister(GetComponent<AudioListener>());
    }

    private void OnDisable()
    {
        inputActions.FindActionMap(ActionMap).Disable();
        AudioManager.AudioLisnerDelelete();
    }

    private void Awake()
    {
        var playerInput = GetComponentInParent<PlayerInput>();
        _moveAction = playerInput.actions.FindAction("Move");
        _interactAction = playerInput.actions.FindAction("Interact");
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponentInChildren<Animator>();
        _animator.speed = animationSpeed;
        _speedController = new SpeedController(acceleration, deceleration, maxWalkSpeed);

        _animationHashes ??= (Enum.GetValues(typeof(ActionType)) as IEnumerable<ActionType>)
            .Where(x => x != ActionType.None)
            .Select(x => new KeyValuePair<ActionType, int>(x, Animator.StringToHash($"{playerType}_{x}")))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    private void Update()
    {
        if (IsBusy)
        {
            _speedController.HandleAcceleration(false);
            return;
        }

        var moveAmount = _moveAction.ReadValue<Vector2>();
        var isMoving = moveAmount != Vector2.zero;

        if (!IsBusy && _interactAction.IsPressed())
            HandleOnInteract();

        _speedController.HandleAcceleration(isMoving);

        if (isMoving)
            _currentDirection = new Vector3(moveAmount.x, 0, moveAmount.y).normalized;
    }

    private void FixedUpdate()
    {
        UpdateAnimator(_speedController.HaveAnySpeed && ApplyMovement());
    }

    private void HandleOnInteract()
    {
        var position = _rigidbody.position - new Vector3(0, 1, 0);
        var direction = _currentDirection * 1f;
        Debug.DrawRay(position, direction, Color.chartreuse);

        var isHit = Physics.Raycast(
            position,
            direction.normalized,
            out var hit,
            RayDistance,
            LayerMask.GetMask("Interactable"),
            QueryTriggerInteraction.Collide);

        if (isHit && hit.IsInteractable(out var interactable))
            interactable?.Interact(playerType, HandleAction, isBusy => IsBusy = isBusy);
    }

    private bool HandleAction(ActionType actionType)
    {
        if (actionType == ActionType.None || !_animationHashes.TryGetValue(actionType, out var hash))
            return false;

        _animator.SetTrigger(hash);

        return true;
    }

    private void UpdateAnimator(bool isWalking)
    {
        if (isWalking != _isWalking)
        {
            _animator.SetBool(_isWalkingHash, isWalking);
            _animator.SetTrigger(isWalking ? _startWalkHash : _stopWalkiHash);

        }

        _isWalking = isWalking;
    }

    private bool ApplyMovement()
    {
        const float rayDistance = 0.4f;

        Vector3 desiredMovement = _currentDirection * (_speedController.Current * Time.fixedDeltaTime);
        Vector3 newPosition = _rigidbody.position + desiredMovement;

        if (_currentDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_currentDirection);
            _rigidbody.rotation = Quaternion.Slerp(_rigidbody.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (!Physics.Raycast(_rigidbody.position, _currentDirection, rayDistance))
        {
            // no collision - full speed
            _rigidbody.MovePosition(newPosition);
            return true;
        }

        // X
        Vector3 slideXMovement = new Vector3(desiredMovement.x, 0, 0);

        if (slideXMovement.magnitude > 0.001f && !Physics.Raycast(_rigidbody.position, slideXMovement.normalized, rayDistance))
        {
            newPosition = _rigidbody.position + slideXMovement;
            _rigidbody.MovePosition(newPosition);
            return true;
        }

        // Z
        Vector3 slideZMovement = new Vector3(0, 0, desiredMovement.z);

        if (slideZMovement.magnitude > 0.001f && !Physics.Raycast(_rigidbody.position, slideZMovement.normalized, rayDistance))
        {
            newPosition = _rigidbody.position + slideZMovement;
            _rigidbody.MovePosition(newPosition);
            return true;
        }
        return false;
    }
}

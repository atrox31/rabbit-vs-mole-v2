using PlayerManagementSystem.AIBehaviour.Common;
using RabbitVsMole;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using Unity.Behavior;
using Unity.Properties;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "MoveToTarget", story: "Move [AvatarOfPlayer] to [Target]", category: "Action", id: "ef9d90629189a0f1bb5a2522ac10978d")]
public partial class MoveToTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> AvatarOfPlayer;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    
    private PlayerAvatar _playerAvatar;
    private GameObject _target;
    private float _playerAvatarInteractionRadius;

    private NavMeshPath _path;

    private bool _isTargetFarmField;
    private bool _isTargetSupplyObject;
    private bool _isTargetOtherPlayer;

    private bool IsTargetStaticPosition => (_isTargetFarmField || _isTargetSupplyObject);
    private Vector3 TargetStaticPosition;

    private Vector3 _selfPosition;
    private Vector3 _targetPosition;
    
    protected override Status OnStart()
    {
        if (!BlackboardManager.SetupVariable(out _playerAvatar, AvatarOfPlayer))
            return Status.Failure;

        if (!BlackboardManager.SetupVariable(out _target, Target))
            return Status.Failure;

        _path = new NavMeshPath();

        _isTargetFarmField = _target.TryGetComponent<FarmFieldBase>(out _);
        _isTargetSupplyObject = _target.CompareTag(AIConsts.SUPPLY_TAG);
        
        if (_target.TryGetComponent<PlayerAvatar>(out var targetPlayerAvatar))
        {
            _isTargetOtherPlayer = targetPlayerAvatar != _playerAvatar;
        }

        if (IsTargetStaticPosition)
        {
            TargetStaticPosition = _target.transform.position;
        }

        GetAvatarInteractonRange(out _playerAvatarInteractionRadius);

        DebugPathStatus();
        return Status.Running;
    }

    private void GetAvatarInteractonRange(out float interactionRangeVariable)
    {
        interactionRangeVariable = 0.5f;

        if (_playerAvatar.gameObject.TryGetComponent(out SphereCollider collider))
        {
            _playerAvatarInteractionRadius = collider.radius;
            return;
        }

        if (_playerAvatar.gameObject.TryGetComponent(out CapsuleCollider collider2))
        {
            _playerAvatarInteractionRadius = collider2.radius;
            return;
        }

    }

    void DebugPathStatus()
    {
        var selfPosition = _playerAvatar.transform.position;
        var targetPosition = IsTargetStaticPosition
            ? TargetStaticPosition
            : _target.transform.position;

        if (_isTargetSupplyObject)
        {
            AIDebugOutput.LogMessage($"MoveToTarget {Target?.Name}");
        }
        if (_isTargetFarmField)
        {
            _target.TryGetComponent<FarmFieldBase>(out FarmFieldBase farmfield);
            AIDebugOutput.LogMessage($"MoveToTarget {farmfield?.StateName}");
        }

        switch (CalculatePath(selfPosition, targetPosition))
        {
            case NavMeshPathStatus.PathComplete:
                AIDebugOutput.LogMessage("Path is reachable!");
                break;
            case NavMeshPathStatus.PathPartial:
                AIDebugOutput.LogWarning("Path is blocked, but I can get closer.");
                break;
            case NavMeshPathStatus.PathInvalid:
                AIDebugOutput.LogError("Path is invalid");
                break;
            default:
                break;
        }
    }

    protected override Status OnUpdate()
    {
        _selfPosition = _playerAvatar.transform.position;
        _targetPosition = IsTargetStaticPosition 
            ? TargetStaticPosition
            : _target.transform.position;

        var status = CalculatePath(_selfPosition, _targetPosition);
        if(status == NavMeshPathStatus.PathInvalid)
        {
            AIDebugOutput.LogError("NavMeshPathStatus.PathInvalid");
            return Status.Failure;
        }

        if (_isTargetSupplyObject)
            return HandleSupplyObject();

        if (_isTargetFarmField)
            return HandleFarmField();

        if (_isTargetOtherPlayer)
            return HandleOtherPlayer();

        return Status.Failure;
    }

    private Status HandleOtherPlayer()
    {
        if (_playerAvatar.IsEnemyInRange)
        {
            _playerAvatar.SetMoveInput(Vector3.zero);
            return Status.Success;
        }
        else
        {
            var pointsOnPath = _path.corners.Length;
            if (pointsOnPath >= 1)
            {
                return MoveToTarget(_path.corners[1]);
            }
        }
        return Status.Failure;
    }

    private bool VectorDistanceMagnitude(Vector3 pos1, Vector3 pos2, float distance)
    {
        Vector2 diff = new Vector2(pos1.x - pos2.x, pos1.z - pos2.z);
        return diff.sqrMagnitude < (distance * distance);
    }

    private Status HandleFarmField()
    {
        const float minimuDistance = 0.5f;
        if (VectorDistanceMagnitude(_selfPosition, _targetPosition, minimuDistance))
        {
            AIDebugOutput.LogMessage("Destynation reached");
            if (_playerAvatar.IsInteractionAvableDown)
            {
                _playerAvatar.SetMoveInput(Vector3.zero);
                return Status.Success;
            }
            else
            {
                // rotate to object until is in reach
                return MoveToTarget(_targetPosition);
            }
        }
        else
        {
            var pointsOnPath = _path.corners.Length;
            if (pointsOnPath >= 1)
            {
                return MoveToTarget(_path.corners[1]);
            }
        }
        return Status.Failure;
    }

    Status HandleSupplyObject()
    {
        float interactiveRadius;
        if (_target.TryGetComponent(out CapsuleCollider capsuleCollider))
        {
            interactiveRadius = capsuleCollider.radius + _playerAvatarInteractionRadius;
        }
        else
        {
            AIDebugOutput.LogError("Cannot get CapsuleCollider in target");
            return Status.Failure;
        }

        if (VectorDistanceMagnitude(_selfPosition, _targetPosition, interactiveRadius))
        {
            if (_playerAvatar.IsInteractionAvableFront)
            {
                AIDebugOutput.LogMessage("Destynation reached");
                _playerAvatar.SetMoveInput(Vector3.zero);
                return Status.Success;
            }
            else
            {
                // rotate to object until is in reach
                AIDebugOutput.LogMessage($"rotate to object until is in reach");
                return MoveToTarget(_targetPosition);
            }
        }
        else
        {
            var pointsOnPath = _path.corners.Length;
            if (pointsOnPath >= 1)
            {
                //AIDebugOutput.LogMessage($"Move to target, distance: {distance:F2}");
                return MoveToTarget(_path.corners[1]);
            }
            else
            {
                AIDebugOutput.LogError("Somethings wrong, corners == 1");
                return Status.Failure;
            }
        }
    }

    private Status MoveToTarget(Vector3 target)
    {
        var moveVector = GetNormalizedDirection2D(_selfPosition, target);
        _playerAvatar.SetMoveInput(moveVector);

        return Status.Running;
    }
    public static Vector2 GetNormalizedDirection2D(Vector3 self, Vector3 target)
    {
        Vector3 direction3D = target - self;
        Vector2 direction2D = new Vector2(direction3D.x, direction3D.z);
        return direction2D.normalized;
    }

    private NavMeshPathStatus CalculatePath(Vector3 startPos, Vector3 targetPos)
    {
        Vector3 startPosition = startPos;
        Vector3 targetPosition = targetPos; 
        
        // Próbuj znaleźć pozycję na NavMesh z większym promieniem
        const float maxSampleDistance = 3.0f;
        
        if (NavMesh.SamplePosition(startPosition, out NavMeshHit startHit, maxSampleDistance, NavMesh.AllAreas))
        {
            startPosition = startHit.position;
        }
        else
        {
            AIDebugOutput.LogError($"Cannot find NavMesh position for start position: {startPosition}. Distance to NavMesh: {startHit.distance}");
            return NavMeshPathStatus.PathInvalid;
        }

        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit targetHit, maxSampleDistance, NavMesh.AllAreas))
        {
            targetPosition = targetHit.position;
        }
        else
        {
            AIDebugOutput.LogError($"Cannot find NavMesh position for target position: {targetPosition}. Distance to NavMesh: {targetHit.distance}");
            return NavMeshPathStatus.PathInvalid;
        }

        NavMesh.CalculatePath(startPosition, targetPosition, NavMesh.AllAreas, _path);
        return _path.status;
    }

}



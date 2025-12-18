using RabbitVsMole;
using GameObjects.FarmField;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "MoveToTarget", story: "Move [AvatarOfPlayer] to [Target]", category: "Action", id: "ef9d90629189a0f1bb5a2522ac10978d")]
public partial class MoveToTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> AvatarOfPlayer;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    
    private NavMeshPath _path;
    private PlayerAvatar _playerAvatar;
    private GameObject _targetObject;
    private IInteractable _targetInteractable;
    private FarmField _targetFarmField;
    private bool _isTargetFarmField;
    
    protected override Status OnStart()
    {
        if (AvatarOfPlayer == null || AvatarOfPlayer.Value == null)
        {
            return Status.Failure;
        }

        _playerAvatar = AvatarOfPlayer.Value.GetComponent<PlayerAvatar>();
        if (_playerAvatar == null)
        {
            return Status.Failure;
        }

        _targetObject = Target?.Value;
        _targetInteractable = _targetObject?.GetComponent<IInteractable>();
        _targetFarmField = _targetObject?.GetComponent<FarmField>();
        _isTargetFarmField = _targetFarmField != null;
        _path = new NavMeshPath();
        
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        // Validate references
        if (AvatarOfPlayer?.Value == null || Target?.Value == null || _playerAvatar == null)
        {
            return Status.Failure;
        }

        // Check if target changed
        if (_targetObject != Target.Value)
        {
            _targetObject = Target.Value;
            _targetInteractable = _targetObject?.GetComponent<IInteractable>();
            _targetFarmField = _targetObject?.GetComponent<FarmField>();
            _isTargetFarmField = _targetFarmField != null;
        }

        Vector3 currentPos = AvatarOfPlayer.Value.transform.position;
        Vector3 targetCenterPos = GetTargetCenterPosition();

        // For FarmField, check distance to center directly
        if (_isTargetFarmField)
        {
            float distanceToCenter = Vector3.Distance(currentPos, targetCenterPos);
            
            // Use smaller threshold to avoid triggering on nearby fields
            // Fields are typically spaced ~1.2 units apart, so 0.8 should only trigger on the actual target
            if (distanceToCenter < 0.8f)
            {
                _playerAvatar.SetMoveInput(Vector2.zero);
                return Status.Success;
            }
        }
        else
        {
            // For objects with colliders, use NearbyInteractable check
            if (_targetInteractable != null && _playerAvatar.NearbyInteractable == _targetInteractable)
            {
                return RotateTowardsTarget();
            }
        }

        // Get positions and validate NavMesh
        Vector3 targetPos = targetCenterPos;

        // For FarmField, find nearest NavMesh point to center and use that as path target
        // Then move towards actual center after reaching NavMesh point
        if (!_isTargetFarmField)
        {
            // Validate and snap positions to NavMesh for non-FarmField targets
            if (!ValidateAndSnapToNavMesh(ref currentPos, ref targetPos))
            {
                _playerAvatar.SetMoveInput(Vector2.zero);
                return Status.Failure;
            }
        }
        else
        {
            // For FarmField, snap start position to NavMesh
            NavMeshHit startHit;
            if (!NavMesh.SamplePosition(currentPos, out startHit, 5f, NavMesh.AllAreas))
            {
                _playerAvatar.SetMoveInput(Vector2.zero);
                return Status.Failure;
            }
            currentPos = startHit.position;
            
            // Find nearest NavMesh point to FarmField center
            NavMeshHit centerHit;
            if (NavMesh.SamplePosition(targetCenterPos, out centerHit, 5f, NavMesh.AllAreas))
            {
                // Use nearest NavMesh point as path target
                targetPos = centerHit.position;
            }
            else
            {
                // If center is not on NavMesh, use center directly (path calculation will handle it)
                targetPos = targetCenterPos;
            }
        }

        // Calculate path
        if (!CalculatePath(currentPos, targetPos))
        {
            _playerAvatar.SetMoveInput(Vector2.zero);
            return Status.Failure;
        }

        // Move along path
        return MoveAlongPath();
    }

    private bool ValidateAndSnapToNavMesh(ref Vector3 startPos, ref Vector3 targetPos)
    {
        NavMeshHit startHit, targetHit;
        if (!NavMesh.SamplePosition(startPos, out startHit, 5f, NavMesh.AllAreas))
        {
            return false;
        }
        if (!NavMesh.SamplePosition(targetPos, out targetHit, 5f, NavMesh.AllAreas))
        {
            return false;
        }
        startPos = startHit.position;
        targetPos = targetHit.position;
        return true;
    }

    private bool CalculatePath(Vector3 startPos, Vector3 targetPos)
    {
        if (!NavMesh.CalculatePath(startPos, targetPos, NavMesh.AllAreas, _path))
        {
            return false;
        }

        if (_path.status == NavMeshPathStatus.PathInvalid)
        {
            return false;
        }

        return true;
    }

    private Vector3 GetTargetCenterPosition()
    {
        if (_isTargetFarmField && _targetFarmField != null)
        {
            // For FarmField, use transform.position as center
            return _targetFarmField.transform.position;
        }
        else if (Target.Value != null)
        {
            // For other objects, try to get center from collider bounds or use position
            Collider col = Target.Value.GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                return col.bounds.center;
            }
            return Target.Value.transform.position;
        }
        return Vector3.zero;
    }

    private Status MoveAlongPath()
    {
        if (_path.corners.Length < 2)
        {
            // Already at target or no path
            return RotateTowardsTarget();
        }

        Vector3 currentPos = AvatarOfPlayer.Value.transform.position;
        
        // For FarmField, check distance to center directly
        if (_isTargetFarmField)
        {
            Vector3 targetCenter = GetTargetCenterPosition();
            float distanceToCenter = Vector3.Distance(currentPos, targetCenter);
            
            // Use smaller threshold (0.8 units) to avoid triggering on nearby fields
            if (distanceToCenter < 0.8f)
            {
                _playerAvatar.SetMoveInput(Vector2.zero);
                return Status.Success;
            }
            
            // If near the path end (NavMesh point), move directly towards FarmField center
            Vector3 pathEnd = _path.corners[_path.corners.Length - 1];
            float distanceToPathEnd = Vector3.Distance(currentPos, pathEnd);
            
            // If close to path end, move directly towards FarmField center
            if (distanceToPathEnd < 1.0f)
            {
                Vector3 directionToCenter = (targetCenter - currentPos);
                directionToCenter.y = 0f;
                
                if (directionToCenter.magnitude > 0.1f)
                {
                    directionToCenter.Normalize();
                    _playerAvatar.SetMoveInput(new Vector2(directionToCenter.x, directionToCenter.z));
                    return Status.Running;
                }
                else
                {
                    // Already at center, stop
                    _playerAvatar.SetMoveInput(Vector2.zero);
                    return Status.Success;
                }
            }
        }
        else
        {
            // For other objects, check distance to path end
            Vector3 targetPos = _path.corners[_path.corners.Length - 1];
            float distanceToTarget = Vector3.Distance(currentPos, targetPos);

            // If close to target, rotate towards it instead of moving
            if (distanceToTarget < 1.0f)
            {
                return RotateTowardsTarget();
            }
        }

        // Find next waypoint
        Vector3 nextPoint = _path.corners[1];
        for (int i = 1; i < _path.corners.Length; i++)
        {
            float distToCorner = Vector3.Distance(currentPos, _path.corners[i]);
            if (distToCorner > 0.2f) // Skip corners that are too close
            {
                nextPoint = _path.corners[i];
                break;
            }
        }

        // Calculate movement direction
        Vector3 direction = (nextPoint - currentPos);
        if (direction.magnitude < 0.01f)
        {
            return RotateTowardsTarget();
        }

        direction.Normalize();
        _playerAvatar.SetMoveInput(new Vector2(direction.x, direction.z));
        return Status.Running;
    }

    private Status RotateTowardsTarget()
    {
        if (Target.Value == null)
        {
            _playerAvatar.SetMoveInput(Vector2.zero);
            return Status.Success;
        }

        Vector3 currentPos = AvatarOfPlayer.Value.transform.position;
        Vector3 targetPos = GetTargetCenterPosition();
        Vector3 directionToTarget = (targetPos - currentPos);
        
        // Remove Y component for horizontal rotation
        directionToTarget.y = 0f;
        
        if (directionToTarget.magnitude < 0.01f)
        {
            _playerAvatar.SetMoveInput(Vector2.zero);
            return Status.Success;
        }

        directionToTarget.Normalize();

        // Get forward direction
        Vector3 forward = AvatarOfPlayer.Value.transform.forward;
        forward.y = 0f;
        forward.Normalize();

        // Check if we're facing the target (dot product close to 1)
        float dotProduct = Vector3.Dot(forward, directionToTarget);
        if (dotProduct > 0.95f) // ~18 degrees tolerance
        {
            _playerAvatar.SetMoveInput(Vector2.zero);
            return Status.Success;
        }

        // Calculate which way to rotate using cross product
        Vector3 cross = Vector3.Cross(forward, directionToTarget);
        float rotationDirection = cross.y; // Positive = rotate right, negative = rotate left

        // Create rotation input (strafe direction with small magnitude to rotate without moving forward much)
        Vector2 forward2D = _playerAvatar.GetForwardVector2();
        Vector2 right2D = new Vector2(forward2D.y, -forward2D.x); // 90 degrees clockwise (right)
        Vector2 left2D = new Vector2(-forward2D.y, forward2D.x); // 90 degrees counter-clockwise (left)
        
        // Use small input in strafe direction to rotate towards target
        Vector2 rotationInput = (rotationDirection > 0 ? right2D : left2D) * 0.3f;
        _playerAvatar.SetMoveInput(rotationInput);
        
        return Status.Running;
    }
}



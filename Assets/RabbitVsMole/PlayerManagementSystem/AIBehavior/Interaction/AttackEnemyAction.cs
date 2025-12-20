using PlayerManagementSystem.AIBehaviour.Common;
using RabbitVsMole;
using System;
using Unity.Behavior;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AttackEnemy", story: "Try [AvatarOfPlayer] to attack enemy", category: "Action", id: "651475aaa8858a6b379d0fb51c3f2e3c")]
public partial class AttackEnemyAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> AvatarOfPlayer;
    private PlayerAvatar _playerAvatar;
    protected override Status OnStart()
    {
        if (AvatarOfPlayer.Value == null || !AvatarOfPlayer.Value.TryGetComponent(out _playerAvatar))
        {
            AIDebugOutput.LogError("PlayerAvatar not found");
            return Status.Failure;
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        AIDebugOutput.LogMessage("Try to attack!");
        if(!_playerAvatar.IsEnemyInRange)
            return Status.Failure;

        if (_playerAvatar.TryActionSpecial())
        {
            AIDebugOutput.LogMessage("Success");
            return Status.Success;
        }
        else
        {
            AIDebugOutput.LogMessage("Failure");
            return Status.Failure;
        }
    }

}


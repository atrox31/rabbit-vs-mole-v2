using PlayerManagementSystem.AIBehaviour.Common;
using RabbitVsMole;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "InteractionFront", story: "[AvatarOfPlayer] try to interact with front object and increment [int] value", category: "Action", id: "da692d53f644660a5f75b671f9d3449f")]
public partial class InteractionFrontAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> AvatarOfPlayer;
    [SerializeReference] public BlackboardVariable<int> Int;
    PlayerAvatar _playerAvatar;

    protected override Status OnStart()
    {
        if (!BlackboardManager.SetupVariable(out _playerAvatar, AvatarOfPlayer))
            return Status.Failure;

        if (_playerAvatar.TryActionFront())
            return Status.Running;
        else
            return Status.Failure;
    }

    protected override Status OnUpdate()
    {
        if (_playerAvatar.IsPerformingAction)
            return Status.Running;

        Int.Value += 3;
        return Status.Success;
    }
}


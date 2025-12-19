using PlayerManagementSystem.AIBehaviour.Common;
using RabbitVsMole;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "InteractDown", story: "[AvatarOfPlayer] try to interact with object under legs and increment [int] value", category: "Action", id: "f1021c1d3ba5264d9aa2d40496de935a")]
public partial class InteractDownAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> AvatarOfPlayer;
    [SerializeReference] public BlackboardVariable<int> Int;
    PlayerAvatar _playerAvatar;

    protected override Status OnStart()
    {
        if (!BlackboardManager.SetupVariable(out _playerAvatar, AvatarOfPlayer))
            return Status.Failure;

        if (_playerAvatar.TryActionDown())
            return Status.Running;
        else
            return Status.Failure;
    }

    protected override Status OnUpdate()
    {
        if (_playerAvatar.IsPerformingAction)
            return Status.Running;

        Int.Value -= 1;
        return Status.Success;
    }
}


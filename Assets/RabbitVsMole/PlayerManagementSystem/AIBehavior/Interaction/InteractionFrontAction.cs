using PlayerManagementSystem.AIBehaviour.Common;
using RabbitVsMole;
using System;
using Unity.Behavior;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "InteractionFront", story: "[AvatarOfPlayer] try to interact with front object", category: "Action", id: "da692d53f644660a5f75b671f9d3449f")]
public partial class InteractionFrontAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> AvatarOfPlayer;
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

        if (GameObject.TryGetComponent<BehaviorGraphAgent>(out var agent))
        {
            var blackboard = agent.BlackboardReference;
            switch (_playerAvatar.PlayerType)
            {
                case PlayerType.Rabbit:
                    blackboard.SetVariableValue("SeedCount", _playerAvatar.Backpack.Seed.Count);
                    blackboard.SetVariableValue("WaterCount", _playerAvatar.Backpack.Water.Count);
                    break;
                case PlayerType.Mole:
                    break;
            }
            return Status.Success;
        }

        return Status.Failure;
    }
}


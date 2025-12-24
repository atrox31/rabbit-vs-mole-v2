using PlayerManagementSystem.AIBehaviour.Common;
using PlayerManagementSystem.Backpack;
using RabbitVsMole;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "InteractDown", story: "[AvatarOfPlayer] try to interact with object under legs", category: "Action", id: "f1021c1d3ba5264d9aa2d40496de935a")]
public partial class InteractDownAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> AvatarOfPlayer;
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

        if (GameObject.TryGetComponent<BehaviorGraphAgent>(out var agent))
        {
            var blackboard = agent.BlackboardReference;
            blackboard.SetVariableValue("HaveCarrot", _playerAvatar.IsHaveCarrot);

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


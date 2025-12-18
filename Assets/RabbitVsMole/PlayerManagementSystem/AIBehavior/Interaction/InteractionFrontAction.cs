using RabbitVsMole;
using System;
using System.Collections;
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
    bool _isWaitComplite;

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

        _playerAvatar.StartCoroutine(Waiter());
        return Status.Running;

        if (_playerAvatar.TryActionFront())
        {
            _playerAvatar.StartCoroutine(Waiter());
            return Status.Running;
        }
        else
            return Status.Failure;
    }

    IEnumerator Waiter()
    {
        yield return new WaitForSeconds(3);
        _isWaitComplite = true;
    }

    protected override Status OnUpdate()
    {
        if (_playerAvatar.IsPerformingAction && !_isWaitComplite)
            return Status.Running;

        Int.Value += 3;
        return Status.Success;
    }
}


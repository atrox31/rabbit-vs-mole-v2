using PlayerManagementSystem.AIBehaviour.Common;
using RabbitVsMole;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CalmDown", story: "Calm down after attack", category: "Action", id: "7342f70623aaf6c3fb864959c7145267")]
public partial class CalmDownAction : Action
{
    private BotAgentController _agent;

    protected override Status OnStart()
    {

        if (!GameObject.TryGetComponent<BotAgentController>(out _agent))
        {
            AIDebugOutput.LogError("BotAgentController not found");
            return Status.Failure;
        }
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        AIDebugOutput.LogMessage("Calm down...");
        _agent.CalmDown();
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}


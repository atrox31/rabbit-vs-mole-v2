using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Increse value", story: "Increse [Variable] by [Value]", category: "Action", id: "d71ee8c389d30db7ba5c977085d91d35")]
public partial class IncreseValueAction : Action
{
    [SerializeReference] public BlackboardVariable<int> Variable;
    [SerializeReference] public BlackboardVariable<int> Value;

    protected override Status OnStart()
    {
        if (Variable == null || Value == null)
        {
            return Status.Failure;
        }
        Variable.ObjectValue = Value.ObjectValue;
        return Status.Success;
    }

}


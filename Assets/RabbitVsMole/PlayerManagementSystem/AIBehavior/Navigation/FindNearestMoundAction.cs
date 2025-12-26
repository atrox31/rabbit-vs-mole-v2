using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "FindNearestMound", story: "Find [NearestMound]", category: "Action", id: "c349d1f3ee715a6ff42e4e3a0f141df4")]
public partial class FindNearestMoundAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> NearestMound;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}


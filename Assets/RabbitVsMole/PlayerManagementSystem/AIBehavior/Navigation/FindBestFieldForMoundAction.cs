using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Find best field for mound", story: "FInd best [TargetField] for mound", category: "Action", id: "2d3d05b1167501bea86a154bcb22f7d1")]
public partial class FindBestFieldForMoundAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> TargetField;

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


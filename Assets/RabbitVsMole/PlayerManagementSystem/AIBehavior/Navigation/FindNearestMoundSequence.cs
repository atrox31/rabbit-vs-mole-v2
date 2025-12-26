using System;
using Unity.Behavior;
using UnityEngine;
using Composite = Unity.Behavior.Composite;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Find nearest Mound", story: "Find [NearestMound]", category: "Flow", id: "63097bedadba7717bd8b7c9734e2c4cd")]
public partial class FindNearestMoundSequence : Composite
{
    [SerializeReference] public BlackboardVariable<GameObject> NearestMound;
    [SerializeReference] public Node Exists;
    [SerializeReference] public Node Notfound;
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


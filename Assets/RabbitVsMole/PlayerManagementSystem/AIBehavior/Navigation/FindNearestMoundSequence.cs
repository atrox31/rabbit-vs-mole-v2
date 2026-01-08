using System;
using Unity.Behavior;
using UnityEngine;
using Composite = Unity.Behavior.Composite;
using Unity.Properties;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System.Linq;
using RabbitVsMole.InteractableGameObject.Field;
using System.Collections.Generic;
using Extensions;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Find nearest Mound", story: "Find [NearestMound] for [AvatarOfPlayer]", category: "Flow", id: "63097bedadba7717bd8b7c9734e2c4cd")]
public partial class FindNearestMoundSequence : Composite
{
    [SerializeReference] public BlackboardVariable<GameObject> NearestMound;
    [SerializeReference] public BlackboardVariable<GameObject> AvatarOfPlayer;
    [SerializeReference] public Node Exists;
    [SerializeReference] public Node Notfound;
    protected override Status OnStart()
    {
        if (AvatarOfPlayer.Value == null)
        {
            NearestMound.Value = null;
        }
        else
        {
            // Find potential targets - using FindObjectsByType is fine for occasional checks
            List<FarmFieldBase> mounds = GameObject.FindObjectsByType<FarmFieldBase>(FindObjectsSortMode.None)
                .Where(field => field.State is FarmFieldMounded)
                .ToList();

            FarmFieldBase nearest = AvatarOfPlayer.Value.FindNearest<FarmFieldBase>(mounds);

            if (nearest != null)
                NearestMound.Value = nearest.gameObject;
            else
                NearestMound.Value = null;
        }

        // Start the appropriate child node based on whether a mound was found
        if (NearestMound.Value != null)
        {
            return StartNode(Exists);
        }
        else
        {
            return StartNode(Notfound);
        }
    }

    protected override Status OnUpdate()
    {
        // OnUpdate is called while children are running
        // The Composite base class handles the child node updates
        return Status.Running;
    }

    protected override void OnEnd()
    {
    }

}


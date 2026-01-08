using RabbitVsMole;
using RabbitVsMole.InteractableGameObject.Field;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Find best field for mound", story: "FInd best [TargetField] for mound for [AvatarOfPlayer] based on if [isUnderground]", category: "Action", id: "2d3d05b1167501bea86a154bcb22f7d1")]
public partial class FindBestFieldForMoundAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> TargetField;
    [SerializeReference] public BlackboardVariable<GameObject> AvatarOfPlayer;
    [SerializeReference] public BlackboardVariable<bool> IsUnderground;
    [SerializeReference] public BlackboardVariable<bool> Cooperation;
    [SerializeReference] public BlackboardVariable<int> Inteligence;
    PlayerAvatar PlayerAvatar;
    protected override Status OnStart()
    {
        TargetField.Value = IsUnderground.Value
            ? FindUndergroundField()
            : FindFarmField();

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return TargetField.Value == null 
            ? Status.Failure
            : Status.Success;
    }

    protected override void OnEnd()
    {
    }

    GameObject FindUndergroundField()
    {
        var allFields = GameObject.FindObjectsByType<UndergroundFieldBase>(FindObjectsSortMode.None);
        if (allFields == null || allFields.Length == 0) return null;

        List<Component> priority1 = new(); // Full grown carrots
        List<Component> priority2 = new(); // Walls
        List<Component> priority3 = new(); // Others (Clean/Growing)

        var playerAvatar = AvatarOfPlayer.Value.GetComponent<PlayerAvatar>();
        bool hasBackpackSpace = playerAvatar.Backpack.Dirt.CanInsert(GameManager.CurrentGameStats.WallDirtCollectPerAction);

        foreach (var field in allFields)
        {
            var state = field.State;

            if (state is UndergroundFieldCarrot && field.IsCarrotReady)
            {
                priority1.Add(field);
                continue;
            }

            if (hasBackpackSpace && state is UndergroundFieldWall or UndergroundFieldMounded)
            {
                priority2.Add(field);
                continue;
            }

            if (state is UndergroundFieldClean || (state is UndergroundFieldCarrot && !field.IsCarrotReady))
            {
                priority3.Add(field);
            }
        }

        // Combine lists in order of importance
        List<Component> finalFields = new(priority1.Count + priority2.Count + priority3.Count);
        finalFields.AddRange(priority1);
        finalFields.AddRange(priority2);
        finalFields.AddRange(priority3);

        if (finalFields.Count == 0)
            return null;

        return SelectFieldByIntelligence(finalFields);
    }

    GameObject FindFarmField()
    {
        List<FarmFieldBase> fields = new();
        if (Cooperation.Value)
        {
            fields = GameObject.FindObjectsByType<FarmFieldBase>(FindObjectsSortMode.None)
                   .Where(field => field.State is FarmFieldRooted)
                   .ToList();
        }
        else
        {
            fields = GameObject.FindObjectsByType<FarmFieldBase>(FindObjectsSortMode.None)
                   .Where(field => field.State is FarmFieldClean or FarmFieldPlanted || (field.State is FarmFieldWithCarrot && !field.IsCarrotReady))
                   .ToList();
        }

        if (fields.Count == 0)
            return null;

        return SelectFieldByIntelligence(fields.Cast<Component>().ToList());
    }

    GameObject SelectFieldByIntelligence(List<Component> fields)
    {
        if (fields.Count == 0)
            return null;

        if (fields.Count == 1)
            return fields[0].gameObject;

        if (AvatarOfPlayer?.Value == null)
            return fields[0].gameObject;

        // Intelligence (0-100), default 50 for mole
        int intelligence = Inteligence?.Value ?? 50;
        intelligence = Mathf.Clamp(intelligence, 0, 100);

        // Sort fields by distance from the avatar (ascending: index 0 is closest)
        Vector3 avatarPosition = AvatarOfPlayer.Value.transform.position;
        fields.Sort((a, b) => 
            Vector3.Distance(avatarPosition, a.transform.position)
            .CompareTo(Vector3.Distance(avatarPosition, b.transform.position)));

        // Intelligence factor: 0 = farthest, 100 = nearest
        // Invert the intelligence factor so 0 picks from the end, 100 picks from the start
        float intelligenceFactor = intelligence / 100f;
        
        // targetIndex: 0 (closest) for 100 intelligence, last (farthest) for 0 intelligence
        float targetIndexFloat = (1f - intelligenceFactor) * (fields.Count - 1);
        
        // Add some randomness: allow spread around the optimal index (±15% of list)
        float searchRange = Mathf.Max(1f, fields.Count * 0.15f);
        int lowerBound = Mathf.FloorToInt(targetIndexFloat - searchRange);
        int upperBound = Mathf.CeilToInt(targetIndexFloat + searchRange);
        
        int selectedIndex = UnityEngine.Random.Range(lowerBound, upperBound + 1);
        selectedIndex = Mathf.Clamp(selectedIndex, 0, fields.Count - 1);

        return fields[selectedIndex].gameObject;
    }
}


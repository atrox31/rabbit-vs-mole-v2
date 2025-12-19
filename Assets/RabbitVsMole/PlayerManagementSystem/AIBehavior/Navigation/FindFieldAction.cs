using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using GameObjects.FarmField;
using GameObjects.FarmField.States;
using RabbitVsMole;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "FindField", story: "Find and set [TargetField] , [AvatarOfPlayer] use [inteligence]", category: "Action", id: "4912b451ab0e9419ba94dd2a442a2e51")]
public partial class FindFieldAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> TargetField;
    [SerializeReference] public BlackboardVariable<GameObject> AvatarOfPlayer;
    [SerializeReference] public BlackboardVariable<int> Inteligence;
    PlayerAvatar _playerAvatar;

    class FarmFieldData
    {
        public FarmField field;
        public int priority;
        public float distance;
        public FarmFieldData(FarmField field, int priority, float distance)
        {
            this.field = field;
            this.priority = priority;
            this.distance = distance;
        }
    }

    protected override Status OnStart()
    {
        if (AvatarOfPlayer == null || AvatarOfPlayer.Value == null)
        {
            Debug.LogError("FindFieldAction: PlayerAvatar is null");
            return Status.Failure;
        }

        _playerAvatar = AvatarOfPlayer.Value.GetComponent<PlayerAvatar>();
        if (_playerAvatar == null)
        {
            return Status.Failure;
        }

        // Find all farm fields in the scene
        FarmField[] allFields = GameObject.FindObjectsByType<FarmField>(FindObjectsSortMode.None);
        
        if (allFields.Length == 0)
        {
            Debug.LogWarning("FindFieldAction: No farm fields found in scene");
            TargetField.Value = null;
            return Status.Failure;
        }

        // Count fields by state for priority modifiers
        int rootedFieldCount = allFields.Count(f => f.State is RootedField);
        int moundedFieldCount = allFields.Count(f => f.State is MoundedField);
        int plantedFieldCount = allFields.Count(f => f.State is PlantedField);
        int grownFieldCount = allFields.Count(f => f.State is GrownField);
        int untouchedFieldCount = allFields.Count(f => f.State is UntouchedField);

        // Calculate base priority for each field
        List<FarmFieldData> fieldData = new List<FarmFieldData>();
        Vector3 agentPosition = _playerAvatar.transform.position;

        foreach (FarmField field in allFields)
        {
            int fieldCount = field.State switch
            {
                RootedField => rootedFieldCount,
                MoundedField => moundedFieldCount,
                PlantedField => plantedFieldCount,
                GrownField => grownFieldCount,
                UntouchedField => untouchedFieldCount,
                _ => 0
            };

            int basePriority = field.State.AIPriority.GetEffectivePriority(fieldCount);

            float distance = Vector3.Distance(agentPosition, field.transform.position);
            fieldData.Add(new (field, basePriority, distance));
        }

        // Get intelligence value (default to 50 if not set)
        int intelligence = Inteligence?.Value ?? 50;
        intelligence = Mathf.Clamp(intelligence, 0, 100);

        // Select field based on intelligence
        GameObject selectedField = SelectField(fieldData, intelligence);
        
        if (selectedField != null)
        {
            TargetField.Value = selectedField;
            return Status.Success;
        }

        TargetField.Value = null;
        return Status.Failure;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }

    private GameObject SelectField(List<FarmFieldData> fieldData, int intelligence)
    {
        if (fieldData.Count == 0) return null;

        // Intelligence 100 = best choice (highest priority, closest)
        // Intelligence 0 = worst choice (lowest priority, farthest)
        // Intelligence 50 = balanced choice

        // Normalize intelligence to 0-1 range (0 = worst, 1 = best)
        float intelligenceFactor = intelligence / 100f;

        // Group fields by priority and sort by priority (highest first)
        List<IGrouping<int, FarmFieldData>> groupedByPriority = fieldData.GroupBy(f => f.priority)
            .OrderByDescending(g => g.Key)
            .ToList();

        // Determine which priority group to select from based on intelligence
        // Intelligence 100 = always highest priority group
        // Intelligence 0 = always lowest priority group
        // Intelligence 50 = weighted selection towards middle
        int selectedPriorityGroupIndex = GetIndexBaseOnInteligence(intelligenceFactor, groupedByPriority.Count);

        var selectedGroup = groupedByPriority[selectedPriorityGroupIndex];
        var fieldsInGroup = selectedGroup.ToList();

        if (fieldsInGroup.Count == 1)
        {
            return fieldsInGroup[0].field.gameObject;
        }

        // Multiple fields with same priority - choose based on distance
        // Higher intelligence = closer, lower intelligence = farther
        fieldsInGroup.Sort((a, b) => a.distance.CompareTo(b.distance));

        // Select index based on intelligence
        // Intelligence 100 = index 0 (closest)
        // Intelligence 0 = last index (farthest)
        // Intelligence 50 = middle index
        int selectedIndex= GetIndexBaseOnInteligence(intelligenceFactor, fieldsInGroup.Count);
        return fieldsInGroup[selectedIndex].field.gameObject;
    }

    private static int GetIndexBaseOnInteligence(float intelligenceFactor, int fieldsInGroupCount)
    {
        int selectedIndex;

        if (intelligenceFactor >= 98f)
        {
            // Perfect intelligence: always highest priority
            return 0;
        }
        else if (intelligenceFactor <= 2f)
        {
            // Worst intelligence: always lowest priority
            return fieldsInGroupCount - 1;
        }

        // Map intelligence to index: 100 -> 0, 50 -> middle, 0 -> last
        float targetIndex = (1f - intelligenceFactor) * (fieldsInGroupCount - 1);

        // Add some randomness based on how far from 50 we are
        // Closer to 50 = more random, closer to extremes = more deterministic
        float randomness = Mathf.Abs(intelligenceFactor - 0.5f) * 2f;
        float randomOffset = (UnityEngine.Random.Range(0f, 1f) - 0.5f) * (1f - randomness) * (fieldsInGroupCount * 0.3f);

        var unclampedSelectedIndex = Mathf.RoundToInt(targetIndex + randomOffset);
        selectedIndex = Mathf.Clamp(unclampedSelectedIndex, 0, fieldsInGroupCount - 1);
        return selectedIndex;
    }
}


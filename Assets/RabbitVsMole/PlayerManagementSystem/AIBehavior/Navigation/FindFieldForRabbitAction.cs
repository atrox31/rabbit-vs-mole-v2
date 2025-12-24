using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using RabbitVsMole;
using RabbitVsMole.InteractableGameObject.Field.Base;
using RabbitVsMole.InteractableGameObject.Base;
using RabbitVsMole.InteractableGameObject.Field;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "FindFieldforRabbit", story: "Find and set [TargetField] for Rabbit, [AvatarOfPlayer] use [inteligence]", category: "Action", id: "4912b451ab0e9419ba94dd2a442a2e51")]
public partial class FindFieldForRabbitAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> TargetField;
    [SerializeReference] public BlackboardVariable<GameObject> AvatarOfPlayer;
    [SerializeReference] public BlackboardVariable<int> Inteligence;
    PlayerAvatar _playerAvatar;

    class FieldData
    {
        public FieldBase field;
        public int priority;
        public float distance;
        public FieldData(FieldBase field, int priority, float distance)
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

        // Find all fields in the scene (both farm and underground fields)
        FarmFieldBase[] allFields = GameObject.FindObjectsByType<FarmFieldBase>(FindObjectsSortMode.None);
        
        if (allFields.Length == 0)
        {
            Debug.LogWarning("FindFieldAction: No fields found in scene");
            TargetField.Value = null;
            return Status.Failure;
        }

        // Count fields by state type for priority modifiers
        var stateTypeCounts = new Dictionary<Type, int>();
        foreach (var field in allFields)
        {
            var stateType = field.State.GetType();
            stateTypeCounts.TryGetValue(stateType, out int count);
            stateTypeCounts[stateType] = count + 1;
        }

        // Calculate base priority for each field
        List<FieldData> fieldData = new List<FieldData>();
        Vector3 agentPosition = _playerAvatar.transform.position;

        foreach (FieldBase field in allFields)
        {
            var stateType = field.State.GetType();
            int fieldCount = stateTypeCounts.TryGetValue(stateType, out int count) ? count : 0;

            if (field.State.AIPriority == null)
            {
                Debug.LogWarning($"FindFieldAction: Field {field.name} has null AIPriority");
                continue;
            }

            int basePriority = field.State.AIPriority.GetEffectivePriority(fieldCount);

            float distance = Vector3.Distance(agentPosition, field.transform.position);
            fieldData.Add(new FieldData(field, basePriority, distance));
        }

        if (fieldData.Count == 0)
        {
            Debug.LogWarning("FindFieldAction: No valid fields with AIPriority found");
            TargetField.Value = null;
            return Status.Failure;
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

    private GameObject SelectField(List<FieldData> fieldData, int intelligence)
    {
        if (fieldData.Count == 0) return null;

        // Intelligence 100 = best choice (highest priority, closest)
        // Intelligence 0 = worst choice (lowest priority, farthest)
        // Intelligence 50 = balanced choice

        // Normalize intelligence to 0-1 range (0 = worst, 1 = best)
        float intelligenceFactor = intelligence / 100f;

        // Group fields by priority and sort by priority (highest first)
        List<IGrouping<int, FieldData>> groupedByPriority = fieldData.GroupBy(f => f.priority)
            .OrderByDescending(g => g.Key)
            .ToList();

        // Determine which priority group to select from based on intelligence
        // Intelligence 100 = always highest priority group (index 0)
        // Intelligence 0 = always lowest priority group (last index)
        // Intelligence 50 = weighted selection towards middle
        int selectedPriorityGroupIndex = GetPriorityGroupIndex(intelligenceFactor, groupedByPriority.Count);

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
        int selectedIndex = GetDistanceIndex(intelligenceFactor, fieldsInGroup.Count);
        return fieldsInGroup[selectedIndex].field.gameObject;
    }

    /// <summary>
    /// Gets the priority group index based on intelligence.
    /// Higher intelligence selects higher priority groups (lower index).
    /// </summary>
    private static int GetPriorityGroupIndex(float intelligenceFactor, int groupCount)
    {
        if (groupCount <= 1) return 0;

        // Intelligence 100 (1.0) -> index 0 (highest priority)
        // Intelligence 0 (0.0) -> last index (lowest priority)
        // Intelligence 50 (0.5) -> middle index
        
        // Invert: higher intelligence = lower index (better priority)
        float targetIndex = (1f - intelligenceFactor) * (groupCount - 1);
        
        // Add controlled randomness - less randomness for extreme intelligence values
        // At intelligence 50, allow more variation; at 0 or 100, be more deterministic
        // For high intelligence (80+), be very deterministic to select best options
        float randomnessFactor = Mathf.Abs(intelligenceFactor - 0.5f) * 2f; // 0 at 50, 1 at extremes
        
        // Reduce max offset for high intelligence values to ensure better selection
        // Intelligence 80+ should have minimal randomness (max 5% offset)
        // Intelligence 50 should have moderate randomness (max 15% offset)
        float maxOffsetPercentage = intelligenceFactor >= 0.8f ? 0.05f : 
                                    intelligenceFactor <= 0.2f ? 0.05f : 0.15f;
        float maxRandomOffset = (1f - randomnessFactor) * (groupCount * maxOffsetPercentage);
        float randomOffset = (UnityEngine.Random.Range(0f, 1f) - 0.5f) * maxRandomOffset;
        
        int selectedIndex = Mathf.RoundToInt(targetIndex + randomOffset);
        return Mathf.Clamp(selectedIndex, 0, groupCount - 1);
    }

    /// <summary>
    /// Gets the distance-based index within a priority group.
    /// Higher intelligence selects closer fields (lower index).
    /// </summary>
    private static int GetDistanceIndex(float intelligenceFactor, int fieldCount)
    {
        if (fieldCount <= 1) return 0;

        // Intelligence 100 (1.0) -> index 0 (closest)
        // Intelligence 0 (0.0) -> last index (farthest)
        // Intelligence 50 (0.5) -> middle index
        
        // Invert: higher intelligence = lower index (closer)
        float targetIndex = (1f - intelligenceFactor) * (fieldCount - 1);
        
        // Add controlled randomness - less randomness for extreme intelligence values
        // For high intelligence (80+), be very deterministic to select closest fields
        float randomnessFactor = Mathf.Abs(intelligenceFactor - 0.5f) * 2f;
        
        // Reduce max offset for high intelligence values
        float maxOffsetPercentage = intelligenceFactor >= 0.8f ? 0.05f : 
                                    intelligenceFactor <= 0.2f ? 0.05f : 0.15f;
        float maxRandomOffset = (1f - randomnessFactor) * (fieldCount * maxOffsetPercentage);
        float randomOffset = (UnityEngine.Random.Range(0f, 1f) - 0.5f) * maxRandomOffset;
        
        int selectedIndex = Mathf.RoundToInt(targetIndex + randomOffset);
        return Mathf.Clamp(selectedIndex, 0, fieldCount - 1);
    }
}


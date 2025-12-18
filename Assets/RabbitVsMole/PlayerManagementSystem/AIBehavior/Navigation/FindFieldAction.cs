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

    protected override Status OnStart()
    {
        if (AvatarOfPlayer.Value == null)
        {
            Debug.LogError("FindFieldAction: PlayerAvatar is null");
            return Status.Failure;
        }
        _playerAvatar = AvatarOfPlayer.Value.GetComponent<PlayerAvatar>();

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

        // Calculate base priority for each field
        List<(FarmField field, float priority, float distance)> fieldData = new List<(FarmField, float, float)>();
        Vector3 agentPosition = _playerAvatar.transform.position;

        foreach (var field in allFields)
        {
            float basePriority = GetBasePriority(field.State);
            
            // Apply count-based modifiers
            if (field.State is RootedField && rootedFieldCount > 3)
            {
                basePriority = 90f;
            }
            else if (field.State is MoundedField && moundedFieldCount > 3)
            {
                basePriority = 80f;
            }

            float distance = Vector3.Distance(agentPosition, field.transform.position);
            fieldData.Add((field, basePriority, distance));
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

    private float GetBasePriority(IFarmFieldState state)
    {
        if (state is GrownField) return 100f;
        if (state is PlantedField) return 70f;
        if (state is MoundedField) return 60f;
        if (state is UntouchedField) return 60f;
        if (state is RootedField) return 50f;
        return 0f;
    }

    private GameObject SelectField(List<(FarmField field, float priority, float distance)> fieldData, int intelligence)
    {
        if (fieldData.Count == 0) return null;

        // Intelligence 100 = best choice (highest priority, closest)
        // Intelligence 0 = worst choice (lowest priority, farthest)
        // Intelligence 50 = balanced choice

        // Normalize intelligence to 0-1 range (0 = worst, 1 = best)
        float intelligenceFactor = intelligence / 100f;

        // Group fields by priority and sort by priority (highest first)
        var groupedByPriority = fieldData.GroupBy(f => f.priority)
            .OrderByDescending(g => g.Key)
            .ToList();

        // Determine which priority group to select from based on intelligence
        // Intelligence 100 = always highest priority group
        // Intelligence 0 = always lowest priority group
        // Intelligence 50 = weighted selection towards middle
        int selectedPriorityGroupIndex;
        
        if (intelligence >= 100)
        {
            // Perfect intelligence: always highest priority
            selectedPriorityGroupIndex = 0;
        }
        else if (intelligence <= 0)
        {
            // Worst intelligence: always lowest priority
            selectedPriorityGroupIndex = groupedByPriority.Count - 1;
        }
        else
        {
            // Weighted selection based on intelligence
            // Map intelligence [0-100] to selection index [worst to best]
            // Use a curve that makes extreme values more likely to pick extremes
            float normalizedIntelligence = intelligenceFactor;
            
            // For intelligence 50, we want balanced selection
            // For intelligence > 50, bias towards higher priority
            // For intelligence < 50, bias towards lower priority
            
            // Calculate target group index (0 = best, count-1 = worst)
            float targetIndex = (1f - normalizedIntelligence) * (groupedByPriority.Count - 1);
            
            // Add some randomness based on how far from 50 we are
            // Closer to 50 = more random, closer to extremes = more deterministic
            float randomness = Mathf.Abs(normalizedIntelligence - 0.5f) * 2f; // 0 at 50, 1 at extremes
            float randomOffset = (UnityEngine.Random.Range(0f, 1f) - 0.5f) * (1f - randomness) * (groupedByPriority.Count * 0.5f);
            
            selectedPriorityGroupIndex = Mathf.RoundToInt(targetIndex + randomOffset);
            selectedPriorityGroupIndex = Mathf.Clamp(selectedPriorityGroupIndex, 0, groupedByPriority.Count - 1);
        }

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
        int selectedIndex;
        if (intelligence >= 100)
        {
            selectedIndex = 0; // Closest
        }
        else if (intelligence <= 0)
        {
            selectedIndex = fieldsInGroup.Count - 1; // Farthest
        }
        else
        {
            // Map intelligence to index: 100 -> 0, 50 -> middle, 0 -> last
            float targetIndex = (1f - intelligenceFactor) * (fieldsInGroup.Count - 1);
            
            // Add randomness based on distance from 50
            float randomness = Mathf.Abs(intelligenceFactor - 0.5f) * 2f;
            float randomOffset = (UnityEngine.Random.Range(0f, 1f) - 0.5f) * (1f - randomness) * (fieldsInGroup.Count * 0.3f);
            
            selectedIndex = Mathf.RoundToInt(targetIndex + randomOffset);
            selectedIndex = Mathf.Clamp(selectedIndex, 0, fieldsInGroup.Count - 1);
        }

        return fieldsInGroup[selectedIndex].field.gameObject;
    }
}


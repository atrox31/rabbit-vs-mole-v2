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
using RabbitVsMole.InteractableGameObject.Storages;

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
        public float distanceToStorage;
        public FieldData(FieldBase field, int priority, float distanceToStorage)
        {
            this.field = field;
            this.priority = priority;
            this.distanceToStorage = distanceToStorage;
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

        // Find all farm fields
        FarmFieldBase[] allFields = GameObject.FindObjectsByType<FarmFieldBase>(FindObjectsSortMode.None);
        
        if (allFields.Length == 0)
        {
            TargetField.Value = null;
            return Status.Failure;
        }

        // Find carrot storage for distance reference (Rabbit's "home")
        FarmCarrotStorage carrotStorage = GameObject.FindObjectsByType<FarmCarrotStorage>(FindObjectsSortMode.None).First();
        Vector3 storagePosition = carrotStorage != null ? carrotStorage.transform.position : _playerAvatar.transform.position;

        // Count fields by state type for priority modifiers
        var stateTypeCounts = new Dictionary<Type, int>();
        foreach (var field in allFields)
        {
            var stateType = field.State.GetType();
            if (!stateTypeCounts.ContainsKey(stateType))
                stateTypeCounts[stateType] = 0;
            stateTypeCounts[stateType]++;
        }

        // Gather field data
        List<FieldData> fieldData = new List<FieldData>();
        foreach (FarmFieldBase field in allFields)
        {
            var stateType = field.State.GetType();
            int fieldCount = stateTypeCounts[stateType];

            if (field.State.AIPriority == null) continue;

            int basePriority = field.State.AIPriority.GetEffectivePriority(fieldCount);
            float distToStorage = Vector3.Distance(storagePosition, field.transform.position);
            fieldData.Add(new FieldData(field, basePriority, distToStorage));
        }

        if (fieldData.Count == 0)
        {
            TargetField.Value = null;
            return Status.Failure;
        }

        // Intelligence (0-100), default 80
        int intelligence = Inteligence?.Value ?? 80;
        intelligence = Mathf.Clamp(intelligence, 0, 100);

        // Select field based on intelligence, distance and previous choice
        GameObject selectedField = SelectField(fieldData, intelligence, storagePosition);
        
        if (selectedField != null)
        {
            TargetField.Value = selectedField;
            _playerAvatar.LastTargetField = selectedField; // Track to avoid immediate repetition
            return Status.Success;
        }

        TargetField.Value = null;
        return Status.Failure;
    }

    private GameObject SelectField(List<FieldData> fieldData, int intelligence, Vector3 storagePosition)
    {
        // 0. Priority: If we are already working on a Rooted or Mounded field, FINISH IT.
        if (_playerAvatar.LastTargetField != null)
        {
            var lastFieldData = fieldData.FirstOrDefault(f => f.field.gameObject == _playerAvatar.LastTargetField);
            if (lastFieldData != null && (lastFieldData.field.State is FarmFieldRooted || lastFieldData.field.State is FarmFieldMounded))
            {
                return _playerAvatar.LastTargetField;
            }
        }

        // 1. Avoid repetition: don't pick the same field twice in a row if alternatives exist
        List<FieldData> availableFields = fieldData;

        if (_playerAvatar.LastTargetField != null && fieldData.Count > 1)
        {
            availableFields = fieldData.Where(f => f.field.gameObject != _playerAvatar.LastTargetField).ToList();
        }

        // 2. Intelligence-based decision: Sensible (high priority) vs Neutral (low/medium priority)
        // Statistical chance based on intelligence: 80 intelligence = 80% sensible, 20% neutral
        bool chooseSensible = UnityEngine.Random.Range(0, 100) < intelligence;

        List<FieldData> candidateFields;
        int maxPriority = availableFields.Max(f => f.priority);

        if (chooseSensible)
        {
            // Pick from the best possible priority group
            candidateFields = availableFields.Where(f => f.priority == maxPriority).ToList();
        }
        else
        {
            // Pick from "neutral" options (anything except the absolute best priority)
            candidateFields = availableFields.Where(f => f.priority < maxPriority).ToList();
            
            // If every field has the same priority, just pick any
            if (candidateFields.Count == 0) candidateFields = availableFields;
        }

        if (candidateFields.Count == 1) return candidateFields[0].field.gameObject;

        // 3. Selection based on distance from storage and intelligence
        // Higher intelligence = closer to storage
        // Lower intelligence = further from storage
        
        // Sort by distance to storage (ascending: index 0 is closest)
        candidateFields.Sort((a, b) => a.distanceToStorage.CompareTo(b.distanceToStorage));

        float intelligenceFactor = intelligence / 100f;
        // targetIndex: 0 (closest) for 100 intelligence, last (farthest) for 0 intelligence
        float targetIndexFloat = (1f - intelligenceFactor) * (candidateFields.Count - 1);
        
        // 4. Randomness: allow some spread around the "optimal" index to make it less predictable
        // We'll use a search range that covers up to 30% of the list
        float searchRange = Mathf.Max(1f, candidateFields.Count * 0.15f);
        int lowerBound = Mathf.FloorToInt(targetIndexFloat - searchRange);
        int upperBound = Mathf.CeilToInt(targetIndexFloat + searchRange);
        
        int selectedIndex = UnityEngine.Random.Range(lowerBound, upperBound + 1);
        selectedIndex = Mathf.Clamp(selectedIndex, 0, candidateFields.Count - 1);

        return candidateFields[selectedIndex].field.gameObject;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}


using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using PlayerManagementSystem.AIBehaviour.Common;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Thinking", story: "Thinking. Time is based on [Intelligence]", category: "Action", id: "662d0bc48aa214db9840ff514b7a5d81")]
public partial class ThinkingAction : Action
{
    [SerializeReference] public BlackboardVariable<int> Intelligence; 
    private float _elapsedTime;
    private float _thinkingDuration;
    
    protected override Status OnStart()
    {
        int intelligenceValue;
        if (Intelligence == null)
        {
            // Use average intelligence as default
            intelligenceValue = (AIConsts.MIN_INTELIGENCE + AIConsts.MAX_INTELIGENCE) / 2;
            AIDebugOutput.LogWarning($"Intelligence variable not set, using default value of {intelligenceValue}");
        }
        else
        {
            intelligenceValue = Intelligence.Value;
        }

        // Clamp intelligence to valid range
        intelligenceValue = Mathf.Clamp(intelligenceValue, AIConsts.MIN_INTELIGENCE, AIConsts.MAX_INTELIGENCE);
        
        // Calculate thinking duration based on intelligence (higher intelligence = shorter thinking time)
        // Map intelligence from [MIN_INTELIGENCE, MAX_INTELIGENCE] to thinking time [MAX_THINKING_TIME, MIN_THINKING_TIME]
        float intelligenceNormalized = (float)(intelligenceValue - AIConsts.MIN_INTELIGENCE) / 
                                      (AIConsts.MAX_INTELIGENCE - AIConsts.MIN_INTELIGENCE);

        _thinkingDuration = Mathf.Lerp(AIConsts.MAX_THINKING_TIME, AIConsts.MIN_THINKING_TIME, intelligenceNormalized);
        _elapsedTime = 0f;
        
        AIDebugOutput.LogMessage($"Thinking for {_thinkingDuration:F2} seconds");
        
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        _elapsedTime += Time.deltaTime;
        
        if (_elapsedTime >= _thinkingDuration)
        {
            return Status.Success;
        }
        
        return Status.Running;
    }

}


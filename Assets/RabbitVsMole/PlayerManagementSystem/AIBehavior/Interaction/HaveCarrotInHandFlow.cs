using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;
using PlayerManagementSystem.AIBehaviour.Common;
using RabbitVsMole;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "HaveCarrotInHand", story: "Check if [AvatarOfPlayer] have Golden Carrot. Flow branches to True or False output.", category: "Flow", id: "0deb05892bc38898f808b4ca56d8eb62")]
public partial class HaveCarrotInHandFlow : Composite
{
    [SerializeReference] public BlackboardVariable<GameObject> AvatarOfPlayer;
    
    private PlayerAvatar _playerAvatar;
    
    // Named children for True and False branches
    [CreateProperty]
    public Dictionary<string, Node> NamedChildren { get; set; }

    protected override Status OnStart()
    {
        if (!BlackboardManager.SetupVariable(out _playerAvatar, AvatarOfPlayer))
        {
            AIDebugOutput.LogError("HaveCarrotInHandFlow: Failed to get PlayerAvatar from AvatarOfPlayer");
            return Status.Failure;
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        // Check the condition
        if (_playerAvatar == null)
        {
            return Status.Failure;
        }

        bool hasCarrot = _playerAvatar.IsHaveCarrot;
        
        AIDebugOutput.LogMessage($"HaveCarrotInHandFlow: Player has carrot = {hasCarrot}");
        
        // Select the appropriate child based on condition
        string childName = hasCarrot ? "True" : "False";
        
        // Execute the selected named child
        // Unity Behavior should automatically populate NamedChildren in the editor
        if (NamedChildren != null && NamedChildren.ContainsKey(childName))
        {
            var child = NamedChildren[childName];
            if (child != null)
            {
                // Use base.OnUpdate() to execute children, but filter to only the selected one
                // First, temporarily modify Children to only contain the selected child
                var originalChildren = Children != null ? new List<Node>(Children) : null;
                
                if (Children != null)
                {
                    Children.Clear();
                    Children.Add(child);
                }
                
                var result = base.OnUpdate();
                
                // Restore original children
                if (originalChildren != null && Children != null)
                {
                    Children.Clear();
                    foreach (var origChild in originalChildren)
                    {
                        Children.Add(origChild);
                    }
                }
                
                return result;
            }
        }
        
        // If child not found, return failure
        AIDebugOutput.LogError($"HaveCarrotInHandFlow: Child '{childName}' not found in NamedChildren");
        return Status.Failure;
    }

    protected override void OnEnd()
    {
    }
}


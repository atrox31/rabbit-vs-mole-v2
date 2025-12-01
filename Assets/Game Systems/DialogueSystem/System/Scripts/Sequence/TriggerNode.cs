using DialogueSystem.Trigger;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem.Nodes
{
    /// <summary>
    /// Node type that executes a trigger during dialogue sequence.
    /// </summary>
    [Serializable]
    public class TriggerNode
    {
        // GUID used by GraphView/Editor to uniquely identify this node
        public string GUID;

        // Position used by GraphView/Editor to draw the node on screen
        public Vector2 EditorPosition;

        // --- TRIGGER CONTENT ---
        [SerializeField] private string _triggerTypeName;
        [SerializeReference] private DialogueTriggerArguments _arguments;

        // --- CONNECTION LOGIC ---
        public List<NodeLink> ExitPorts = new List<NodeLink>();
        
        // Data input connections - maps field names to TriggerDataNode connections
        public List<ArgumentDataConnection> ArgumentDataConnections = new List<ArgumentDataConnection>();

        // Constructor for editor utility
        public TriggerNode(Vector2 position)
        {
            GUID = Guid.NewGuid().ToString();
            EditorPosition = position;
        }

        /// <summary>
        /// Gets the trigger type name.
        /// </summary>
        public string TriggerTypeName => _triggerTypeName;

        /// <summary>
        /// Gets the trigger arguments.
        /// </summary>
        public DialogueTriggerArguments Arguments => _arguments;

        /// <summary>
        /// Sets the trigger type and arguments.
        /// </summary>
        public void SetTrigger(string triggerTypeName, DialogueTriggerArguments arguments)
        {
            _triggerTypeName = triggerTypeName;
            _arguments = arguments;
        }

        /// <summary>
        /// Gets the trigger to execute. Creates trigger instance at runtime.
        /// Resolves data connections from TriggerDataNodes before execution.
        /// </summary>
        public IDialogueTrigger GetTrigger(DialogueSequence sequence = null)
        {
            // Try new system first
            if (!string.IsNullOrEmpty(_triggerTypeName) && _arguments != null)
            {
                try
                {
                    // Resolve data connections if sequence is provided
                    if (sequence != null)
                    {
                        ResolveDataConnections(sequence);
                    }

                    var triggerType = Type.GetType(_triggerTypeName);
                    
                    // If Type.GetType fails, search in all loaded assemblies
                    if (triggerType == null)
                    {
                        // Extract type name without assembly info if it's an AssemblyQualifiedName
                        string typeNameToSearch = _triggerTypeName;
                        int commaIndex = _triggerTypeName.IndexOf(',');
                        if (commaIndex > 0)
                        {
                            typeNameToSearch = _triggerTypeName.Substring(0, commaIndex);
                        }
                        
                        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                        {
                            triggerType = assembly.GetType(typeNameToSearch);
                            if (triggerType != null)
                                break;
                        }
                    }
                    
                    if (triggerType != null && typeof(IDialogueTrigger).IsAssignableFrom(triggerType))
                    {
                        var trigger = Activator.CreateInstance(triggerType) as IDialogueTrigger;
                        if (trigger != null)
                        {
                            trigger.Arguments = _arguments;
                            return trigger;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create trigger of type {_triggerTypeName}: {ex.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves data connections from TriggerDataNodes and applies values to arguments.
        /// </summary>
        private void ResolveDataConnections(DialogueSequence sequence)
        {
            if (_arguments == null || ArgumentDataConnections == null || ArgumentDataConnections.Count == 0)
                return;

            var argsType = _arguments.GetType();
            var fields = argsType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            foreach (var connection in ArgumentDataConnections)
            {
                var dataNode = sequence.GetTriggerDataNodeByGUID(connection.SourceDataNodeGUID);
                if (dataNode == null) continue;

                var value = dataNode.GetOutputValue(connection.SourcePortName, sequence);
                if (value == null) continue;

                var field = System.Array.Find(fields, f => f.Name == connection.TargetFieldName);
                if (field != null)
                {
                    try
                    {
                        // Convert value to field type if needed
                        var fieldType = field.FieldType;
                        if (value.GetType() != fieldType)
                        {
                            value = Convert.ChangeType(value, fieldType);
                        }
                        field.SetValue(_arguments, value);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"TriggerNode.ResolveDataConnections: Failed to set field '{connection.TargetFieldName}': {ex.Message}");
                    }
                }
            }
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(_triggerTypeName))
            {
                return $"[TriggerNode] Trigger: {_triggerTypeName}";
            }
            var trigger = GetTrigger();
            return $"[TriggerNode] Trigger: {(trigger != null ? trigger.GetType().Name : "None")}";
        }
    }
}


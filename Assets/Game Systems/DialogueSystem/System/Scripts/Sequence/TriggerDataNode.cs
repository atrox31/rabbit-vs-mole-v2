using System;
using System.Collections.Generic;
using DialogueSystem.TriggerData;
using UnityEngine;

namespace DialogueSystem.Nodes
{
    /// <summary>
    /// Node type that provides data to trigger arguments.
    /// </summary>
    [Serializable]
    public class TriggerDataNode
    {
        // GUID used by GraphView/Editor to uniquely identify this node
        public string GUID;

        // Position used by GraphView/Editor to draw the node on screen
        public Vector2 EditorPosition;

        // --- TRIGGER DATA CONTENT ---
        [SerializeField] private string _dataTypeName;
        [SerializeReference] private object _data;

        // --- CONNECTION LOGIC ---
        // Output ports - each port represents a data output
        public List<DataPort> OutputPorts = new List<DataPort>();

        // Constructor for editor utility
        public TriggerDataNode(Vector2 position)
        {
            GUID = Guid.NewGuid().ToString();
            EditorPosition = position;
        }

        /// <summary>
        /// Gets the data type name.
        /// </summary>
        public string DataTypeName => _dataTypeName;

        /// <summary>
        /// Gets the data.
        /// </summary>
        public object Data => _data;

        /// <summary>
        /// Sets the data type and data.
        /// </summary>
        public void SetData(string dataTypeName, object data)
        {
            _dataTypeName = dataTypeName;
            _data = data;
        }

        /// <summary>
        /// Gets a value from a specific output port at runtime.
        /// </summary>
        public object GetOutputValue(string portName, DialogueSequence sequence = null)
        {
            if (string.IsNullOrEmpty(_dataTypeName) || _data == null)
                return null;

            try
            {
                var dataType = Type.GetType(_dataTypeName);
                if (dataType == null)
                {
                    foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        int commaIndex = _dataTypeName.IndexOf(',');
                        string typeNameToSearch = commaIndex > 0 ? _dataTypeName.Substring(0, commaIndex) : _dataTypeName;
                        dataType = assembly.GetType(typeNameToSearch);
                        if (dataType != null) break;
                    }
                }

                if (dataType != null && typeof(ITriggerData).IsAssignableFrom(dataType))
                {
                    // Create instance and set data
                    var triggerData = Activator.CreateInstance(dataType) as ITriggerData;
                    if (triggerData != null)
                    {
                        triggerData.Data = _data;
                        return triggerData.GetOutputValue(portName, sequence);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"TriggerDataNode.GetOutputValue: Failed to get value for port '{portName}': {ex.Message}");
            }

            return null;
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(_dataTypeName))
            {
                return $"[TriggerDataNode] Type: {_dataTypeName}";
            }
            return "[TriggerDataNode] Type: None";
        }
    }

    /// <summary>
    /// Represents a data output port connection.
    /// </summary>
    [Serializable]
    public class DataPort
    {
        public string PortName;
        public string PortType; // Type name as string
        public string TargetNodeGUID; // GUID of the target trigger node
        public string TargetFieldName; // Name of the field in target node arguments
    }

    /// <summary>
    /// Represents a connection from a TriggerDataNode to a TriggerNode argument field.
    /// </summary>
    [Serializable]
    public class ArgumentDataConnection
    {
        public string SourceDataNodeGUID; // GUID of the source TriggerDataNode
        public string SourcePortName; // Name of the output port on the source node
        public string TargetFieldName; // Name of the field in the target TriggerNode arguments
    }
}


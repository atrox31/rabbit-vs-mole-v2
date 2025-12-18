using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DialogueSystem.TriggerData;
using UnityEditor;
using UnityEngine;

namespace DialogueSystem.Editor
{
    /// <summary>
    /// Helper class to find and manage trigger data types.
    /// </summary>
    public static class TriggerDataTypeHelper
    {
        private static Dictionary<Type, Type> _dataTypeToDataMap;
        private static List<Type> _dataTypes;

        static TriggerDataTypeHelper()
        {
            RefreshDataTypes();
        }

        /// <summary>
        /// Refreshes the list of available trigger data types.
        /// </summary>
        public static void RefreshDataTypes()
        {
            _dataTypes = new List<Type>();
            _dataTypeToDataMap = new Dictionary<Type, Type>();

            // Use Unity's TypeCache for efficient type discovery
            var allTypes = TypeCache.GetTypesDerivedFrom<ITriggerData>();
            
            foreach (var dataType in allTypes)
            {
                // Skip abstract classes and interfaces
                if (dataType.IsAbstract || dataType.IsInterface)
                    continue;

                // Skip if it doesn't have a parameterless constructor
                if (dataType.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                _dataTypes.Add(dataType);

                // Try to find the corresponding Data type from TriggerDataBase<TData>
                var baseType = dataType.BaseType;
                while (baseType != null && baseType != typeof(object))
                {
                    if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(TriggerDataBase<>))
                    {
                        var genericArgs = baseType.GetGenericArguments();
                        if (genericArgs.Length > 0)
                        {
                            _dataTypeToDataMap[dataType] = genericArgs[0];
                        }
                        break;
                    }
                    baseType = baseType.BaseType;
                }
            }
        }

        /// <summary>
        /// Gets all available trigger data types.
        /// </summary>
        public static List<Type> GetDataTypes()
        {
            return _dataTypes.ToList();
        }

        /// <summary>
        /// Gets the data type for a given trigger data type.
        /// </summary>
        public static Type GetDataType(Type triggerDataType)
        {
            if (_dataTypeToDataMap.TryGetValue(triggerDataType, out var dataType))
            {
                return dataType;
            }
            return null;
        }

        /// <summary>
        /// Gets the display name for a trigger data type.
        /// </summary>
        public static string GetDataDisplayName(Type triggerDataType)
        {
            if (triggerDataType == null) return "None";
            
            // Remove TD_ prefix
            var name = triggerDataType.Name;
            if (name.StartsWith("TD_"))
            {
                name = name.Substring(3);
            }
            return name;
        }

        /// <summary>
        /// Creates a new instance of data for a trigger data type.
        /// </summary>
        public static object CreateDataInstance(Type triggerDataType)
        {
            var dataType = GetDataType(triggerDataType);
            if (dataType != null)
            {
                return Activator.CreateInstance(dataType);
            }
            return null;
        }

        /// <summary>
        /// Gets the full type name including assembly for serialization.
        /// </summary>
        public static string GetFullTypeName(Type type)
        {
            return type.AssemblyQualifiedName;
        }

        /// <summary>
        /// Gets a type from its full name.
        /// </summary>
        public static Type GetTypeFromFullName(string fullTypeName)
        {
            if (string.IsNullOrEmpty(fullTypeName))
                return null;

            return Type.GetType(fullTypeName);
        }

        /// <summary>
        /// Gets output port names for a trigger data type.
        /// </summary>
        public static string[] GetOutputPortNames(Type triggerDataType)
        {
            if (triggerDataType == null) return new string[0];

            try
            {
                var instance = Activator.CreateInstance(triggerDataType) as ITriggerData;
                if (instance != null)
                {
                    return instance.GetOutputPortNames();
                }
            }
            catch { }

            return new string[0];
        }

        /// <summary>
        /// Gets output port type for a specific port.
        /// </summary>
        public static Type GetOutputPortType(Type triggerDataType, string portName)
        {
            if (triggerDataType == null || string.IsNullOrEmpty(portName)) return null;

            try
            {
                var instance = Activator.CreateInstance(triggerDataType) as ITriggerData;
                if (instance != null)
                {
                    return instance.GetOutputPortType(portName);
                }
            }
            catch { }

            return null;
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DialogueSystem.Trigger;
using UnityEditor;
using UnityEngine;

namespace DialogueSystem.Editor
{
    /// <summary>
    /// Helper class to find and manage trigger types.
    /// </summary>
    public static class TriggerTypeHelper
    {
        private static Dictionary<Type, Type> _triggerToArgumentsMap;
        private static List<Type> _triggerTypes;

        static TriggerTypeHelper()
        {
            RefreshTriggerTypes();
        }

        /// <summary>
        /// Refreshes the list of available trigger types.
        /// </summary>
        public static void RefreshTriggerTypes()
        {
            _triggerTypes = new List<Type>();
            _triggerToArgumentsMap = new Dictionary<Type, Type>();

            // Use Unity's TypeCache for efficient type discovery
            var allTypes = TypeCache.GetTypesDerivedFrom<IDialogueTrigger>();
            
            foreach (var triggerType in allTypes)
            {
                // Skip abstract classes and interfaces
                if (triggerType.IsAbstract || triggerType.IsInterface)
                    continue;

                // Skip if it doesn't have a parameterless constructor
                if (triggerType.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                _triggerTypes.Add(triggerType);

                // Try to find the corresponding Arguments type
                var argsProperty = triggerType.GetProperty("Args", BindingFlags.Public | BindingFlags.Instance);
                if (argsProperty != null)
                {
                    var argsType = argsProperty.PropertyType;
                    if (typeof(DialogueTriggerArguments).IsAssignableFrom(argsType))
                    {
                        _triggerToArgumentsMap[triggerType] = argsType;
                    }
                }
            }
        }

        /// <summary>
        /// Gets all available trigger types.
        /// </summary>
        public static List<Type> GetTriggerTypes()
        {
            return _triggerTypes.ToList();
        }

        /// <summary>
        /// Gets the arguments type for a given trigger type.
        /// </summary>
        public static Type GetArgumentsType(Type triggerType)
        {
            if (_triggerToArgumentsMap.TryGetValue(triggerType, out var argsType))
            {
                return argsType;
            }
            return null;
        }

        /// <summary>
        /// Gets the display name for a trigger type.
        /// </summary>
        public static string GetTriggerDisplayName(Type triggerType)
        {
            if (triggerType == null) return "None";
            
            // Try to get name from Arguments class if available
            var argsType = GetArgumentsType(triggerType);
            if (argsType != null)
            {
                var instance = Activator.CreateInstance(argsType) as DialogueTriggerArguments;
                if (instance != null && !string.IsNullOrEmpty(instance.Name))
                {
                    return instance.Name;
                }
            }

            // Fallback to type name without DT_ prefix
            var name = triggerType.Name;
            if (name.StartsWith("DT_"))
            {
                name = name.Substring(3);
            }
            return name;
        }

        /// <summary>
        /// Creates a new instance of arguments for a trigger type.
        /// </summary>
        public static DialogueTriggerArguments CreateArgumentsInstance(Type triggerType)
        {
            var argsType = GetArgumentsType(triggerType);
            if (argsType != null)
            {
                return Activator.CreateInstance(argsType) as DialogueTriggerArguments;
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
    }
}


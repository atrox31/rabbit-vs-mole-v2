using RabbitVsMole;
using System;
using Unity.Behavior;
using UnityEngine;

namespace PlayerManagementSystem.AIBehaviour.Common
{
    public class BlackboardManager
    {
        public static bool SetupVariable<T, U>(out T varible, BlackboardVariable<U> blackboardVariable)
        {
            varible = default(T);
            if (blackboardVariable == null)
            {
                AIDebugOutput.LogError("blackboardVariable == null");
                return false;
            }
            if (blackboardVariable.Value == null)
            {
                AIDebugOutput.LogError("blackboardVariable.Value == null");
                return false;
            }

            if (blackboardVariable.Value is GameObject go)
            {
                // If T is GameObject, just assign it directly
                if (typeof(T) == typeof(GameObject))
                {
                    varible = (T)(object)go;
                    return true;
                }
                // Otherwise, try to get component of type T
                return go.TryGetComponent(out varible);
            }

            AIDebugOutput.LogError("blackboardVariable.Value != GameObject");
            return false;
        }
    }
}
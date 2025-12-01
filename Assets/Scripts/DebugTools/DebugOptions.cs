#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace DebugTools
{
    public abstract class DebugOptions<T> : Editor where T : MonoBehaviour
    {
        protected void Option(T myScript, string text, Action function, bool argument)
        {
            GUI.enabled = argument;
            if (GUILayout.Button(text)) function();
        }

        protected TResult Option<TResult>(T myScript, string text, Func<TResult> function, bool argument)
        {
            GUI.enabled = argument;
            var result = default(TResult);

            if (GUILayout.Button(text))
                result = function();

            return result;
        }
    }
}
#endif
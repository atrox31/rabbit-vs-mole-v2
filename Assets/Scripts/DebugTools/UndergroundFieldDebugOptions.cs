#if UNITY_EDITOR
using GameObjects;
using UnityEditor;
using UnityEngine;

namespace DebugTools
{
    [CustomEditor(typeof(UndergroundField))]
    public class UndergroundFieldDebugOptions : DebugOptions<UndergroundField>
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var myScript = (UndergroundField)target;

            if (Application.isPlaying)
            {
                GUILayout.Label("Debug options");
                Option(myScript, "Delete wall", myScript.DestroyWallWithAnimation, myScript.HasBlocker);
                EditorGUILayout.Space();
                Option(myScript, "Create Mound", myScript.CreateMound, myScript.CanCreateMound);
                Option(myScript, "Delete Mound", myScript.DestroyMound, myScript.HasMound);
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use this button.", MessageType.Info);
            }
        }
    }
}
#endif
#if UNITY_EDITOR
using GameObjects;
using UnityEditor;
using UnityEngine;

namespace DebugTools
{
    [CustomEditor(typeof(UndergroundWall))]
    public class UndergroundWallDebugOptions : DebugOptions<UndergroundWall>
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var myScript = (UndergroundWall)target;

            if (Application.isPlaying)
            {
                GUILayout.Label("Debug options");
                Option(myScript, "Delete Wall", myScript.DeleteWallImmediately, true);
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use this button.", MessageType.Info);
            }
        }
    }
}
#endif
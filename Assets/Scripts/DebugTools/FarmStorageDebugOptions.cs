#if UNITY_EDITOR
using GameObjects;
using UnityEditor;
using UnityEngine;

namespace DebugTools
{
    [CustomEditor(typeof(FarmStorage))]
    public class FarmStorageDebugOptions : DebugOptions<FarmStorage>
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var myScript = (FarmStorage)target;

            if (Application.isPlaying)
            {
                GUILayout.Label("Debug options");
                Option(myScript, "Spawn Carrot", myScript.AddCarrot, true);
                Option(myScript, "Delete Carrot", myScript.DeleteCarrot, myScript.CanDeleteCarrot);
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use this button.", MessageType.Info);
            }
        }
    }
}
#endif
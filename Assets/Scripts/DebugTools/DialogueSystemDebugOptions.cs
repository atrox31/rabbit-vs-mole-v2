#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace DebugTools
{
	[CustomEditor(typeof(DialogueSystemTest))]
	public class DialogueSystemTestDebugOptions : DebugOptions<DialogueSystemTest>
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			var myScript = (DialogueSystemTest)target;

			if (Application.isPlaying)
			{
				GUILayout.Label("Debug options");
				Option(myScript, "Test selected dialogue", myScript.StartSelectedDialogue, true);
				EditorGUILayout.Space();
			}
			else
			{
				EditorGUILayout.HelpBox("Enter Play Mode to use this button.", MessageType.Info);
			}
		}
	}
}
#endif
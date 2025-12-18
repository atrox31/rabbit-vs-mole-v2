using UnityEditor;
using UnityEngine;

namespace DialogueSystem.Editor
{
    [CustomEditor(typeof(DialogueSequence))]
    public class DialogueSequenceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DialogueSequence sequence = (DialogueSequence)target;

            // Draw default inspector for hidden fields
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("GameObject References", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Assign GameObjects from the scene here. These can be referenced by name in TD_GameObject nodes.", MessageType.Info);

            if (sequence.GameObjectReferences == null)
            {
                sequence.GameObjectReferences = new System.Collections.Generic.List<DialogueSequence.GameObjectReference>();
            }

            for (int i = 0; i < sequence.GameObjectReferences.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                var reference = sequence.GameObjectReferences[i];
                if (reference == null)
                {
                    sequence.GameObjectReferences[i] = new DialogueSequence.GameObjectReference();
                    reference = sequence.GameObjectReferences[i];
                }

                // Name field
                reference.Name = EditorGUILayout.TextField(reference.Name, GUILayout.Width(150));
                
                // GameObject field - allow scene objects only (allowSceneObjects = true)
                reference.GameObject = (GameObject)EditorGUILayout.ObjectField(
                    reference.GameObject,
                    typeof(GameObject),
                    true, // allowSceneObjects = true - allows selecting objects from scene
                    GUILayout.ExpandWidth(true)
                );

                // Remove button
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    sequence.GameObjectReferences.RemoveAt(i);
                    i--;
                    EditorUtility.SetDirty(sequence);
                    continue;
                }

                EditorGUILayout.EndHorizontal();
            }

            // Add button
            if (GUILayout.Button("Add GameObject Reference"))
            {
                sequence.GameObjectReferences.Add(new DialogueSequence.GameObjectReference());
                EditorUtility.SetDirty(sequence);
            }

            EditorGUILayout.Space();
            
            // Button to open the graph editor window
            if (GUILayout.Button("Open Dialogue Graph Editor"))
            {
                OpenEditorWindow(sequence);
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(sequence);
            }
        }

        // Method called from the menu item/button to open the editor
        public static void OpenEditorWindow(DialogueSequence graph)
        {
            // 1. Get the existing window instance or create a new one
            DialogueGraphEditor window = EditorWindow.GetWindow<DialogueGraphEditor>();
            window.titleContent = new GUIContent("Dialogue Graph Editor: " + graph.name);

            // 2. Set the target graph for the editor window
            window.SetTarget(graph);

            // 3. Optional: Focus the window
            window.Focus();
        }
    }
}


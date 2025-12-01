using DialogueSystem;
using DialogueSystem.Editor;
using UnityEditor;
using UnityEngine;

public static class DialogueSequenceAssetCallbacks
{
    // Attribute to intercept double-clicking on the asset
    [UnityEditor.Callbacks.OnOpenAsset(1)]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        // Get the asset object that was clicked
        Object asset = EditorUtility.InstanceIDToObject(instanceID);

        // Check if the clicked object is a DialogueSequence
        if (asset is DialogueSequence dialogueSequence)
        {
            // Open our custom editor window and pass the asset to it
            DialogueSequenceEditor.OpenEditorWindow(dialogueSequence);

            // Return true to tell Unity that we handled the opening (don't use the default inspector/text editor)
            return true;
        }

        // Return false to allow Unity to use its default handler for other assets
        return false;
    }
}
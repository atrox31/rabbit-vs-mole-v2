using UnityEditor;
using UnityEngine;

public static class AmbientCreationTool
{
    // The path "GameObject/Audio/Ambient Sound Source" will put the option 
    // in the Hierarchy context menu (right-click in Hierarchy) and the main GameObject menu.
    // To make it appear in the 'Create' menu in the Project window, we typically 
    // use a ScriptableObject, but for a scene object, this is the standard way.

    // We target the Hierarchy/Scene view creation menu for scene components.
    [MenuItem("GameObject/Audio/Ambient Sound Source", priority = 10)]
    public static void CreateAmbientSoundSource(MenuCommand menuCommand)
    {
        // 1. Create the base GameObject
        GameObject go = new GameObject("Ambient Sound Source");

        // 2. Add the required components
        go.AddComponent<AudioSource>();
        go.AddComponent<AmbientSoundSource>(); // Your custom script

        // 3. Register the creation for Undo/Redo functionality
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        Selection.activeObject = go;
    }
}
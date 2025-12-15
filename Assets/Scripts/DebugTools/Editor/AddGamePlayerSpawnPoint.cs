using UnityEditor;
using UnityEngine;

public static class SpawnPointEditor
{
    // Unity method invoked when the menu item is clicked.
    // The path defines where the option appears: 
    // "GameObject/YourGameName/Spawn Point"
    [MenuItem("GameObject/SpawnPoints/Add Game Player Spawn Point", false, 10)]
    private static void AddGamePlayerSpawnPoint()
    {
        // 1. Create a new empty GameObject
        GameObject go = new GameObject("Game Player Spawn Point");

        // 2. Add the component to the new GameObject
        go.AddComponent<RabbitVsMolePlayerSpawnPoint>();
        go.tag = "spawnPoint";

        // 3. Select the newly created object in the Hierarchy
        Selection.activeObject = go;

        // 4. Optionally register the action for Undo functionality
        Undo.RegisterCreatedObjectUndo(go, "Create Game Player Spawn Point");
    }
}

using GameObjects.System;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevelPhysics;

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
        go.AddComponent<RabbitVsMole.PlayerSpawnPoint>();
        CreateSphere(go.transform);

        // 3. Select the newly created object in the Hierarchy
        go.tag = "spawnPoint";
        Selection.activeObject = go;

        // 4. Optionally register the action for Undo functionality
        Undo.RegisterCreatedObjectUndo(go, "Create Game Player Spawn Point");
    }

    static void CreateSphere(Transform parent)
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.AddComponent<HideOnPlay>();
        sphere.transform.parent = parent.transform;
        sphere.transform.position = new Vector3(0, 0, 0);

        var renderer = sphere.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = new Material(Shader.Find("Custom/WorldSpaceTransparentCheckerboard"));

        var material = renderer.sharedMaterial;
        material.SetFloat("_CheckerboardSize", 0.1f);
        material.SetColor("_Color", new Color(.9f, .2f, .2f, .6f));
    }
}

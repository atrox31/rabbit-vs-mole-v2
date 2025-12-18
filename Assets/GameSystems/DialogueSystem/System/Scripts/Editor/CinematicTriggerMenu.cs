using UnityEngine;
using UnityEditor;

namespace DialogueSystem.Editor
{
    internal static class CinematicTriggerMenu
    {
        private const string MenuPath = "GameObject/Dialogue System/Create Cinematic Trigger Zone";

        [MenuItem(MenuPath, false, 10)]
        private static void CreateCinematicTrigger(MenuCommand menuCommand)
        {
            var go = new GameObject("CinematicTrigger");

            go.AddComponent<DialogueSystem.CinematicTriggerZone>();
            var gobc = go.AddComponent<BoxCollider>();
            gobc.isTrigger = true;

            var parentGameObject = menuCommand.context as GameObject ?? Selection.activeGameObject;

            if (parentGameObject != null)
            {
                GameObjectUtility.SetParentAndAlign(go, parentGameObject);
            }

            Undo.RegisterCreatedObjectUndo(go, "Create Cinematic Trigger");

            Selection.activeGameObject = go;
        }
    }
}
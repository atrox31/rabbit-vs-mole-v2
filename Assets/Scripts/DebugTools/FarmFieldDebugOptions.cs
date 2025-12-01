#if UNITY_EDITOR
using GameObjects.FarmField;
using UnityEditor;
using UnityEngine;

namespace DebugTools
{
    [CustomEditor(typeof(FarmField))]
    public class FarmFieldDebugOptions : DebugOptions<FarmField>
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var myScript = (FarmField)target;

            if (Application.isPlaying)
            {
                GUILayout.Label("Debug options");
                Option(myScript, "Start Water Field", myScript.StartWatering, myScript.CanWaterField);
                Option(myScript, "Stop Water Field", myScript.StopWatering, myScript.IsWatering);
                Option(myScript, "Clear Water", myScript.ClearWater, myScript.HasWater);
                EditorGUILayout.Space();

                Option(myScript, "Plant Seed", myScript.PlantSeed, myScript.CanPlantSeed);
                Option(myScript, "Remove Seed", myScript.DeleteSeed, myScript.HasSeed);
                EditorGUILayout.Space();

                Option(myScript, "Pick Carrot", myScript.HarvestCarrot, myScript.CanHarvestCarrot);
                EditorGUILayout.Space();

                Option(myScript, "Create Mound", myScript.CreateMound, myScript.CanCreateMound);
                Option(myScript, "Delete Mound", myScript.DestroyMound, myScript.HasMound);
                EditorGUILayout.Space();

                Option(myScript, "Plant Roots", myScript.PlantRoots, myScript.CanPlantRoots);
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use this button.", MessageType.Info);
            }
        }
    }
}
#endif
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace WalkingImmersionSystem
{
    public static class WalkingImmersionTools
    {
        // Menu path as requested
        private const string MENU_PATH = "Tools/Walking Immersion System/Set Adressable labels";

        // The type of asset we are looking for (the Scriptable Object)
        private const string ASSET_TYPE_FILTER = "t:TerrainLayerData";

        // The label to assign
        private const string REQUIRED_LABEL = "TerrainDataConfigs";

        [MenuItem(MENU_PATH)]
        public static void SetAdressableLabelsForFolder()
        {
            // 1. Open folder selection dialog
            // Start from the Assets root by default
            string chosenFullPath = EditorUtility.OpenFolderPanel(
                "Wybierz folder z danymi terenu",
                Application.dataPath,
                "");

            // Check if the user selected a path
            if (string.IsNullOrEmpty(chosenFullPath))
            {
                Debug.Log("Operation cancelled by the user.");
                return;
            }

            // 2. Validate and convert the path
            string assetsPathRoot = Application.dataPath;

            // Ensure the path is within the project's Assets folder
            if (!chosenFullPath.Contains(assetsPathRoot))
            {
                EditorUtility.DisplayDialog("B³¹d", "Wybierz folder znajduj¹cy siê wewn¹trz folderu 'Assets' Twojego projektu.", "OK");
                return;
            }

            // Convert full system path to relative Unity path (e.g., Assets/MyFolder)
            string relativeUnityPath = "Assets" + chosenFullPath.Replace(assetsPathRoot, "");

            // 3. Get Addressables settings
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressable Asset Settings not found. Please initialize Addressables system first (Window -> Asset Management -> Addressables -> Groups).");
                return;
            }

            // 4. Find assets in the chosen folder
            // The array takes a list of folders to search.
            string[] assetGuids = AssetDatabase.FindAssets(ASSET_TYPE_FILTER, new[] { relativeUnityPath });

            if (assetGuids.Length == 0)
            {
                EditorUtility.DisplayDialog("Informacja", $"Nie znaleziono ¿adnych obiektów typu 'TerrainLayerData' w folderze: {relativeUnityPath}", "OK");
                return;
            }

            // 5. Assign the label to all found assets
            int labeledCount = 0;
            foreach (string guid in assetGuids)
            {
                // Get the Addressable Asset Entry for the asset
                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);

                if (entry != null)
                {
                    // Add the required label to the entry
                    if (!entry.labels.Contains(REQUIRED_LABEL))
                    {
                        entry.SetLabel(REQUIRED_LABEL, true);
                        labeledCount++;
                    }
                }
            }

            // 6. Save and notify
            // This is necessary to persist the changes in the Addressables configuration
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, settings, true);

            Debug.Log($"SUCCESS: Assigned label '{REQUIRED_LABEL}' to {labeledCount} asset(s) in folder: {relativeUnityPath}");
            EditorUtility.DisplayDialog("Gotowe!", $"Pomyœlnie oznaczono {assetGuids.Length} obiektów typu 'TerrainLayerData' etykiet¹ '{REQUIRED_LABEL}'.", "OK");
        }
    }
}
#endif
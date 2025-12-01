#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;

namespace EditorTools
{
    // Define the path in the top menu bar
    public class GarbageFinderWindow : EditorWindow
    {
        private Dictionary<string, List<GarbageFinder.AssetData>> _unusedAssets = new Dictionary<string, List<GarbageFinder.AssetData>>();
        private Vector2 _scrollPosition;
        private GUIStyle _headerStyle;
        private const string UnusedFolderPath = "Assets/Unused assets";

        // Stores which asset types are selected for scanning
        private Dictionary<string, bool> _assetTypeSelection = new Dictionary<string, bool>()
        {
            {"Prefabs", false},
            {"Models", false},
            {"Textures", false},
            {"Materials", false},
            {"Sounds", false},
            {"ScriptableObjects", false},
            {"Animations", false},
            {"Other", false}
        };

        // Stores the foldout state for each category
        private Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();

        // Add menu item to the Tools menu
        [MenuItem("Tools/GarbageFinder/Find unused assets")]
        public static void ShowWindow()
        {
            // Opens the window or focuses it if already open
            GarbageFinderWindow window = GetWindow<GarbageFinderWindow>("Unused Assets Finder");

            // Set minimum window size (Correction #3)
            window.minSize = new Vector2(900, 600);
        }

        private void OnEnable()
        {
            // Initialize header style for section titles
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.6f, 0.8f, 1.0f) }
            };

            // Initialize foldout states for all categories
            foreach (var key in _assetTypeSelection.Keys)
            {
                if (!_foldoutStates.ContainsKey(key))
                {
                    _foldoutStates.Add(key, true);
                }
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(5);

            // --- ASSET TYPE SELECTION (Correction #1) ---
            EditorGUILayout.LabelField("Select Asset Types to Scan:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            // Display checkboxes for selection
            foreach (var key in new List<string>(_assetTypeSelection.Keys))
            {
                _assetTypeSelection[key] = EditorGUILayout.ToggleLeft(key, _assetTypeSelection[key], GUILayout.Width(120));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // --- FIND BUTTON ---
            if (GUILayout.Button("Scan Project for Unused Assets", GUILayout.Height(30)))
            {
                FindAssets();
            }

            EditorGUILayout.Space(10);

            if (_unusedAssets.Count == 0 || !_unusedAssets.SelectMany(kvp => kvp.Value).Any())
            {
                EditorGUILayout.HelpBox("Press 'Scan Project' to find unused assets. This process may take a while.", MessageType.Info);
                return;
            }

            // --- ASSET LIST ---
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            int totalAssets = 0;
            int totalSelected = 0;

            // Iterate through categories
            foreach (var kvp in _unusedAssets.OrderBy(k => k.Key))
            {
                string category = kvp.Key;
                List<GarbageFinder.AssetData> assets = kvp.Value;

                if (assets.Count == 0) continue;

                totalAssets += assets.Count;
                totalSelected += assets.Count(a => a.IsSelected);

                // Section Header with Foldout (Correction #4)
                _foldoutStates[category] = EditorGUILayout.Foldout(_foldoutStates[category], $"--- {category} ({assets.Count}) ---", true, _headerStyle);

                if (_foldoutStates[category])
                {
                    // Draw the checklist for assets in this category
                    for (int i = 0; i < assets.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();

                        // Toggle (Checklist item)
                        assets[i].IsSelected = EditorGUILayout.Toggle(assets[i].IsSelected, GUILayout.Width(20));

                        // Asset Name and Date Added
                        string nameAndDate = $"{assets[i].Name} (Added: {assets[i].DateAdded})";
                        EditorGUILayout.LabelField(nameAndDate, EditorStyles.label, GUILayout.Width(300));

                        // Path
                        GUIStyle pathStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = false };
                        EditorGUILayout.SelectableLabel(assets[i].Path, pathStyle); // Removed MaxWidth constraint for better scaling

                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.Space(5);
                }
            }

            EditorGUILayout.EndScrollView();

            // --- CLEAN BUTTON AND SUMMARY ---
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"Total Assets Found: {totalAssets}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Assets Selected to Move: {totalSelected}", EditorStyles.boldLabel);

            GUI.enabled = totalSelected > 0;
            if (GUILayout.Button($"Clean Selected Assets ({totalSelected}) - Move to 'Unused assets'", GUILayout.Height(40)))
            {
                CleanAssets();
            }
            GUI.enabled = true;
        }

        private void FindAssets()
        {
            // Pass the selection map to the finder logic
            List<string> selectedCategories = _assetTypeSelection.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
            _unusedAssets = GarbageFinder.FindUnusedAssets(selectedCategories);

            // Ensure all categories found have a foldout state
            foreach (var key in _unusedAssets.Keys)
            {
                if (!_foldoutStates.ContainsKey(key))
                {
                    _foldoutStates.Add(key, true);
                }
            }
        }

        private void CleanAssets()
        {
            // Flatten the list of all selected assets from all categories
            List<GarbageFinder.AssetData> assetsToMove = _unusedAssets.SelectMany(kvp => kvp.Value)
                                                                       .Where(a => a.IsSelected)
                                                                       .ToList();

            if (assetsToMove.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No assets selected to clean.", "OK");
                return;
            }

            // Create the main destination folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder(UnusedFolderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Unused assets");
            }

            int movedCount = 0;

            // Move each selected asset
            foreach (var asset in assetsToMove)
            {
                // Logic to maintain subfolder structure
                string originalDir = Path.GetDirectoryName(asset.Path);
                string relativePath = originalDir.Replace("Assets", "").TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                string targetDir = Path.Combine(UnusedFolderPath, relativePath);

                // Recursively create the target directory if needed
                if (!AssetDatabase.IsValidFolder(targetDir))
                {
                    string currentPath = "Assets/Unused assets";
                    string[] subDirs = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    foreach (var subDir in subDirs.Where(d => !string.IsNullOrEmpty(d)))
                    {
                        string nextPath = Path.Combine(currentPath, subDir);
                        if (!AssetDatabase.IsValidFolder(nextPath))
                        {
                            AssetDatabase.CreateFolder(currentPath, subDir);
                        }
                        currentPath = nextPath;
                    }
                    targetDir = currentPath;
                }

                string fileName = Path.GetFileName(asset.Path);
                string destinationPath = Path.Combine(targetDir, fileName);

                // Move the asset and its associated meta file
                string result = AssetDatabase.MoveAsset(asset.Path, destinationPath);

                if (string.IsNullOrEmpty(result))
                {
                    movedCount++;
                }
                else
                {
                    Debug.LogError($"Failed to move asset {asset.Path} to {destinationPath}. Error: {result}");
                }
            }

            AssetDatabase.Refresh();
            FindAssets(); // Rescan to refresh the list

            EditorUtility.DisplayDialog("Clean Complete", $"Successfully moved {movedCount} assets to '{UnusedFolderPath}'.", "OK");
        }
    }
}
#endif
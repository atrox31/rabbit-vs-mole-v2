#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace EditorTools
{
    // Static class to handle the logic of finding unused assets
    public static class GarbageFinder
    {
        public class AssetData
        {
            public string Name { get; }
            public string Path { get; }
            public string DateAdded { get; }
            public bool IsSelected { get; set; } = true; // Default to selected

            public AssetData(string path)
            {
                Path = path;
                Name = System.IO.Path.GetFileNameWithoutExtension(path);
                IsSelected = true;

                // Get creation time
                if (File.Exists(path))
                {
                    DateAdded = File.GetCreationTime(path).ToShortDateString();
                }
                else
                {
                    DateAdded = "N/A";
                }
            }
        }

        private static readonly string[] IgnoredExtensions = { ".cs", ".js", ".boo", ".meta", ".unity", ".gltf" };

        // This mapping ensures the category name used in the window matches the determined category
        private static string GetAssetCategory(Object asset)
        {
            if (asset is GameObject) return "Prefabs";
            if (asset is Mesh) return "Models";
            if (asset is Texture) return "Textures";
            if (asset is Material) return "Materials";
            if (asset is AudioClip) return "Sounds";
            if (asset is ScriptableObject) return "ScriptableObjects";
            if (asset is AnimationClip) return "Animations";
            return "Other";
        }

        // Accepts a list of categories to filter the results (Correction #1)
        public static Dictionary<string, List<AssetData>> FindUnusedAssets(List<string> categoriesToScan)
        {
            EditorUtility.DisplayProgressBar("Scanning Project", "Collecting all assets...", 0.1f);

            // 1. Get all assets paths that are inside the 'Assets' folder (Correction #2)
            string[] allProjectAssetPaths = AssetDatabase.GetAllAssetPaths()
                .Where(p => p.StartsWith("Assets/") && !p.StartsWith("Assets/Unused assets"))
                .ToArray();

            // Root paths for dependency check (Scenes and Resources content are considered roots)
            HashSet<string> rootPaths = new HashSet<string>(allProjectAssetPaths.Where(p => p.EndsWith(".unity") || p.Contains("/Resources/") || p.Contains("/StreamingAssets/")));

            // 2. Determine dependencies for all root paths (used assets)
            HashSet<string> usedPaths = new HashSet<string>();
            int processed = 0;

            foreach (string rootPath in rootPaths)
            {
                string[] dependencies = AssetDatabase.GetDependencies(rootPath, true);
                foreach (string dep in dependencies)
                {
                    usedPaths.Add(dep);
                }
                processed++;
                EditorUtility.DisplayProgressBar("Scanning Project", $"Analyzing dependencies... ({processed}/{rootPaths.Count})", (float)processed / rootPaths.Count);
            }

            // 3. Filter assets and group into categories
            Dictionary<string, List<AssetData>> unusedAssets = new Dictionary<string, List<AssetData>>();

            foreach (string path in allProjectAssetPaths)
            {
                // Skip assets that are explicitly used or are code files
                if (usedPaths.Contains(path) ||
                    path.Contains("/Editor/") ||
                    IgnoredExtensions.Any(ext => path.ToLower().EndsWith(ext)))
                {
                    continue;
                }

                // Skip special Unity folders
                if (path.StartsWith("ProjectSettings/") ||
                    path.StartsWith("Library/") ||
                    path.StartsWith("Assets/Gizmos") ||
                    path.StartsWith("Assets/Plugins"))
                {
                    continue;
                }

                // Check if the asset is a Directory (folder) in the project structure
                // We check both the AssetDatabase path and the file system to be safe.
                FileAttributes attr = File.GetAttributes(path);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    // Skip project folders, only interested in files
                    continue;
                }

                // Determine asset type
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                string category = GetAssetCategory(asset);

                // Only process selected categories (Correction #1)
                if (!categoriesToScan.Contains(category))
                {
                    continue;
                }

                // Group the asset
                if (!unusedAssets.ContainsKey(category))
                {
                    unusedAssets.Add(category, new List<AssetData>());
                }

                unusedAssets[category].Add(new AssetData(path));
            }

            EditorUtility.ClearProgressBar();
            return unusedAssets;
        }
    }
}
#endif
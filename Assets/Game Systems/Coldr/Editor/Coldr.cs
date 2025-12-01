using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Coldr
{
    [InitializeOnLoad]
    public class Coldr
    {
        private static Dictionary<string, Color> folderColors = new Dictionary<string, Color>();
        
        private const string FOLDER_COLOR_KEY = "Coldr_FolderColor_";

        static Coldr()
        {
            LoadColors();
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
            EditorApplication.projectChanged += LoadColors;
        }
        
        [MenuItem("Assets/Coldr/Cycle Folder Color %f1", false, 20)]
        private static void CycleFolderColor()
        {
            foreach (string guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path))
                {
                    Color currentColor = folderColors.ContainsKey(path) ? folderColors[path] : Color.clear;
                    Color[] currentColorCycle = GetCurrentColorCycle();
                    int currentIndex = System.Array.IndexOf(currentColorCycle, currentColor);
                    int nextIndex = (currentIndex + 1) % currentColorCycle.Length;
                    SetColorForPath(path, currentColorCycle[nextIndex]);
                }
            }
            EditorApplication.RepaintProjectWindow();
        }

        [MenuItem("Assets/Coldr/Clear Folder Color", false, 21)]
        private static void ClearFolderColor()
        {
            foreach (string guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path))
                {
                    SetColorForPath(path, Color.clear);
                }
            }
            EditorApplication.RepaintProjectWindow();
        }

        [MenuItem("Assets/Coldr/Colors/Soft Pink", false, 30)]
        private static void SetSoftPink()
        {
            SetColorForSelectedFolders(new Color(0.85f, 0.65f, 0.75f));
        }

        [MenuItem("Assets/Coldr/Colors/Soft Mint", false, 31)]
        private static void SetSoftMint()
        {
            SetColorForSelectedFolders(new Color(0.65f, 0.85f, 0.75f));
        }

        [MenuItem("Assets/Coldr/Colors/Soft Lavender", false, 32)]
        private static void SetSoftLavender()
        {
            SetColorForSelectedFolders(new Color(0.75f, 0.75f, 0.85f));
        }

        [MenuItem("Assets/Coldr/Colors/Soft Cream", false, 33)]
        private static void SetSoftCream()
        {
            SetColorForSelectedFolders(new Color(0.85f, 0.85f, 0.65f));
        }

        [MenuItem("Assets/Coldr/Colors/Soft Purple", false, 34)]
        private static void SetSoftPurple()
        {
            SetColorForSelectedFolders(new Color(0.75f, 0.65f, 0.85f));
        }

        [MenuItem("Assets/Coldr/Colors/Soft Sky", false, 35)]
        private static void SetSoftSky()
        {
            SetColorForSelectedFolders(new Color(0.65f, 0.75f, 0.85f));
        }

        private static void SetColorForSelectedFolders(Color color)
        {
            foreach (string guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path))
                {
                    SetColorForPath(path, color);
                }
            }
            EditorApplication.RepaintProjectWindow();
        }
        
        private static Color[] GetCurrentColorCycle()
        {
            List<Color> colors = ColdrPreferences.GetAllColors();
            return colors.ToArray();
        }
        
        [MenuItem("Assets/Coldr/Cycle Folder Color %f1", true)]
        [MenuItem("Assets/Coldr/Clear Folder Color", true)]
        private static bool ValidateColorMenu()
        {
            return Selection.assetGUIDs.Length > 0 && AssetDatabase.IsValidFolder(AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]));
        }

        [MenuItem("Assets/Coldr/Colors/Soft Pink", true)]
        [MenuItem("Assets/Coldr/Colors/Soft Mint", true)]
        [MenuItem("Assets/Coldr/Colors/Soft Lavender", true)]
        [MenuItem("Assets/Coldr/Colors/Soft Cream", true)]
        [MenuItem("Assets/Coldr/Colors/Soft Purple", true)]
        [MenuItem("Assets/Coldr/Colors/Soft Sky", true)]
        private static bool ValidateColorSelectionMenu()
        {
            return Selection.assetGUIDs.Length > 0 && AssetDatabase.IsValidFolder(AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]));
        }
        
        private static void SetColorForPath(string path, Color color)
        {
            if (color == Color.clear)
            {
                folderColors.Remove(path);
                EditorPrefs.DeleteKey(FOLDER_COLOR_KEY + path);
            }
            else
            {
                folderColors[path] = color;
                EditorPrefs.SetString(FOLDER_COLOR_KEY + path, ColorUtility.ToHtmlStringRGBA(color));
            }
        }
        
        private static void LoadColors()
        {
            folderColors.Clear();
            string[] allFolders = AssetDatabase.GetAllAssetPaths();
            foreach (string folder in allFolders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    string colorKey = FOLDER_COLOR_KEY + folder;
                    if (EditorPrefs.HasKey(colorKey))
                    {
                        Color color;
                        
                        if (ColorUtility.TryParseHtmlString("#" + EditorPrefs.GetString(colorKey), out color))
                            folderColors[folder] = color;
                    }
                }
            }
        }
        
        private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (folderColors.ContainsKey(path) && AssetDatabase.IsValidFolder(path))
            {
                bool isSmallIcon = selectionRect.height <= 20;
                Rect iconRect;
                
                if (isSmallIcon)
                    iconRect = new Rect(selectionRect.x + 3, selectionRect.y, 16, 16);
                else
                    iconRect = new Rect(selectionRect.x, selectionRect.y, selectionRect.width, selectionRect.width);
                
                Color originalColor = GUI.color;
                GUI.color = folderColors[path];
                
                Texture2D folderIcon = EditorGUIUtility.FindTexture("d_Folder Icon");
                if (folderIcon == null) folderIcon = EditorGUIUtility.FindTexture("Folder Icon");
                if (folderIcon == null) folderIcon = EditorGUIUtility.FindTexture("FolderEmpty Icon");
                
                if (folderIcon != null)
                    GUI.DrawTexture(iconRect, folderIcon);
                
                GUI.color = originalColor;
            }
        }
        
        public static void ClearAllFolderColors()
        {
            folderColors.Clear();
        }
    }
}
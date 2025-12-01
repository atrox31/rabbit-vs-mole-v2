using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Coldr
{
    public class ColdrPreferences : SettingsProvider
    {
        private static List<Color> allColors = new List<Color>();
        private static List<Color> originalBaseColors = new List<Color>();
        private static Vector2 scrollPosition;
        
        private const string COLOR_COUNT_KEY = "Coldr_ColorCount";
        private const string COLOR_KEY = "Coldr_Color_";
        
        public ColdrPreferences(string path, SettingsScope scope = SettingsScope.User) 
            : base(path, scope) { }
        
        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            LoadColors();
            InitializeOriginalBaseColors();
        }
        
        private static void InitializeOriginalBaseColors()
        {
            originalBaseColors = new List<Color>
            {
                new Color(0.85f, 0.65f, 0.75f), // Soft Pink
                new Color(0.65f, 0.85f, 0.75f), // Soft Mint
                new Color(0.75f, 0.75f, 0.85f), // Soft Lavender
                new Color(0.85f, 0.85f, 0.65f), // Soft Cream
                new Color(0.75f, 0.65f, 0.85f), // Soft Purple
                new Color(0.65f, 0.75f, 0.85f)  // Soft Sky
            };
        }
        
        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.LabelField("Coldr Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.LabelField("Shortcut Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("To change the shortcut:\n1. Go to Edit > Shortcuts\n2. Search for 'Coldr/Cycle Folder Color'\n3. Click and set your preferred shortcut", MessageType.Info);
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Current Shortcut:", GUILayout.Width(120));
            EditorGUILayout.LabelField("F1 (Default)", EditorStyles.helpBox);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("All colors in the cycle. You can modify any color and add new ones.", MessageType.Info);
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Add New Color"))
            {
                allColors.Add(Color.white);
                SaveColors();
            }
            
            EditorGUILayout.Space();
            
            for (int i = 0; i < allColors.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                Color newColor = EditorGUILayout.ColorField($"Color {i + 1}", allColors[i]);
                if (newColor != allColors[i])
                {
                    allColors[i] = newColor;
                    SaveColors();
                }
                
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    allColors.RemoveAt(i);
                    SaveColors();
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to Original Colors"))
            {
                if (EditorUtility.DisplayDialog("Reset Colors", 
                    "Are you sure you want to reset all colors to the original default colors?", 
                    "Yes", "No"))
                {
                    allColors.Clear();
                    allColors.AddRange(originalBaseColors);
                    SaveColors();
                }
            }
            
            if (GUILayout.Button("Clear All Folder Colors"))
            {
                if (EditorUtility.DisplayDialog("Clear All Colors", 
                    "Are you sure you want to remove colors from all folders?", 
                    "Yes", "No"))
                {
                    ClearAllFolderColors();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndScrollView();
        }
        
        private static void LoadColors()
        {
            allColors.Clear();
            int colorCount = EditorPrefs.GetInt(COLOR_COUNT_KEY, 0);
            
            if (colorCount == 0)
            {
                InitializeOriginalBaseColors();
                allColors.AddRange(originalBaseColors);
                SaveColors();
            }
            else
            {
                for (int i = 0; i < colorCount; i++)
                {
                    string colorString = EditorPrefs.GetString(COLOR_KEY + i, ColorUtility.ToHtmlStringRGBA(Color.white));
                    Color color;

                    if (ColorUtility.TryParseHtmlString("#" + colorString, out color))
                        allColors.Add(color);
                }
            }
        }
        
        private static void SaveColors()
        {
            EditorPrefs.SetInt(COLOR_COUNT_KEY, allColors.Count);
            
            for (int i = 0; i < allColors.Count; i++)
            {
                EditorPrefs.SetString(COLOR_KEY + i, ColorUtility.ToHtmlStringRGBA(allColors[i]));
            }
        }
        
        private static void ClearAllFolderColors()
        {
            string[] allFolders = AssetDatabase.GetAllAssetPaths();
            
            foreach (string folder in allFolders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                    EditorPrefs.DeleteKey("Coldr_FolderColor_" + folder);
            }
            
            Coldr.ClearAllFolderColors();
            
            EditorApplication.RepaintProjectWindow();
        }
        
        public static List<Color> GetAllColors()
        {
            LoadColors();
            List<Color> colorsWithClear = new List<Color>(allColors);
            colorsWithClear.Add(Color.clear);
            return colorsWithClear;
        }
        
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            var provider = new ColdrPreferences("Preferences/Coldr", SettingsScope.User);
            provider.keywords = GetSearchKeywordsFromGUIContentProperties<ColdrPreferences>();
            return provider;
        }
    }
} 
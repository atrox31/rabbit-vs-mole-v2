using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace WalkingImmersionSystem
{
    // Central manager to load all required sound configurations
    public class SoundConfigLoader : MonoBehaviour
    {
        private static SoundConfigLoader _instance;

        // The specific label assigned to ALL TerrainLayerData SOs
        private const string SoundConfigLabel = "TerrainDataConfigs";
        
        // Fallback path in Resources folder (used when Addressables fail)
        private const string ResourcesFallbackPath = "WalkingImmersion/TerrainLayerData";

        // Public property to hold the loaded data
        private List<TerrainLayerData> _loadedSoundConfigs;

        private AsyncOperationHandle<IList<TerrainLayerData>> _loadHandle;
        private bool _loadedFromAddressables = false;

        private static void CreateInstance()
        {
            var gameOB = new GameObject("SoundConfigLoader");
            _instance = gameOB.AddComponent<SoundConfigLoader>();
            DontDestroyOnLoad(gameOB);

            _instance.InitializeConfigs();
        }

        public static void InitializeLoader()
        {
            if (_instance == null)
                CreateInstance();
        }

        public static List<TerrainLayerData> GetTerrainLayerData()
        {
            if (_instance == null)
                CreateInstance();

            return _instance._loadedSoundConfigs;
        }

        public bool InitializeConfigs()
        {
            // First, try to load from Addressables
            if (TryLoadFromAddressables())
            {
                return true;
            }
            
            // Fallback: try to load from Resources
            Debug.LogWarning($"SoundConfigLoader: Addressables failed, trying Resources fallback from '{ResourcesFallbackPath}'...");
            if (TryLoadFromResources())
            {
                return true;
            }
            
            Debug.LogError($"SoundConfigLoader: Failed to load sound configurations from both Addressables and Resources. " +
                          $"Make sure to either build Addressables before building the game, " +
                          $"or place TerrainLayerData assets in 'Resources/{ResourcesFallbackPath}' folder.");
            _loadedSoundConfigs = new List<TerrainLayerData>();
            return false;
        }
        
        private bool TryLoadFromAddressables()
        {
            try
            {
                // Load ALL assets that share the specified label
                _loadHandle = Addressables.LoadAssetsAsync<TerrainLayerData>(
                    SoundConfigLabel,
                    null);

                _loadHandle.WaitForCompletion();

                if (_loadHandle.Status == AsyncOperationStatus.Succeeded && _loadHandle.Result != null && _loadHandle.Result.Count > 0)
                {
                    // Store the result (a list of all loaded Scriptable Objects)
                    _loadedSoundConfigs = new List<TerrainLayerData>(_loadHandle.Result);
                    _loadedFromAddressables = true;
                    DebugHelper.Log(this, $"Loaded {_loadedSoundConfigs.Count} sound configurations from Addressables.");
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"SoundConfigLoader: Addressables loading exception: {ex.Message}");
            }
            
            return false;
        }
        
        private bool TryLoadFromResources()
        {
            try
            {
                var loadedConfigs = Resources.LoadAll<TerrainLayerData>(ResourcesFallbackPath);
                
                if (loadedConfigs != null && loadedConfigs.Length > 0)
                {
                    _loadedSoundConfigs = loadedConfigs.ToList();
                    _loadedFromAddressables = false;
                    DebugHelper.Log(this, $"Loaded {_loadedSoundConfigs.Count} sound configurations from Resources fallback.");
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"SoundConfigLoader: Resources loading exception: {ex.Message}");
            }
            
            return false;
        }

        private void OnDestroy()
        {
            if (_loadedFromAddressables && _loadHandle.IsValid())
            {
                Addressables.Release(_loadHandle);
                DebugHelper.Log(this, "Released Addressables sound configurations handle.");
            }
        }
    }
}
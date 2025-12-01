using System.Collections.Generic;
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

        // Public property to hold the loaded data
        private List<TerrainLayerData> _loadedSoundConfigs;

        private AsyncOperationHandle<IList<TerrainLayerData>> _loadHandle;

        private static void CreateInstance()
        {
            var gameOB = new GameObject("SoundConfigLoader");
            _instance = gameOB.AddComponent<SoundConfigLoader>();
            DontDestroyOnLoad(gameOB);

            _instance.InitializeConfigsAsync();
        }

        public static List<TerrainLayerData> GetTerrainLayerData()
        {
            if (_instance == null)
                CreateInstance();

            return _instance._loadedSoundConfigs;
        }

        public bool InitializeConfigsAsync()
        {
            // Load ALL assets that share the specified label
            _loadHandle = Addressables.LoadAssetsAsync<TerrainLayerData>(
                SoundConfigLabel,
                null);

            _loadHandle.WaitForCompletion();

            if (_loadHandle.Status == AsyncOperationStatus.Succeeded)
            {
                // Store the result (a list of all loaded Scriptable Objects)
                _loadedSoundConfigs = new List<TerrainLayerData>(_loadHandle.Result);
                Debug.Log($"SoundConfigLoader->InitializeConfigsAsync (SUCCESS): Loaded {_loadedSoundConfigs.Count} sound configuration objects.");
                return true;
            }
            else
            {
                Debug.LogError($"SoundConfigLoader->InitializeConfigsAsync (ERROR): Failed to load sound configurations from label '{SoundConfigLabel}'. Status: {_loadHandle.Status}");
                return false;
            }
        }

        private void OnDestroy()
        {
            if (_loadHandle.IsValid())
            {
                Addressables.Release(_loadHandle);
                Debug.Log("Released Addressables sound configurations handle.");
            }
        }
    }
}
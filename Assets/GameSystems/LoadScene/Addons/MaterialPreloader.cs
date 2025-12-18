using Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;

public static class MaterialPreloader
{
    // Common texture property names used in Unity shaders
    private static readonly string[] CommonTextureProperties = new string[]
    {
        "_MainTex", "_BaseMap", "_BumpMap", "_NormalMap", "_DetailAlbedoMap", 
        "_DetailNormalMap", "_EmissionMap", "_MetallicGlossMap", "_OcclusionMap",
        "_ParallaxMap", "_SpecGlossMap", "_DetailMask", "_NormalTex", "_EmmisTex"
    };

    // Possible texture paths for Addressables lookup
    private static readonly string[] TexturePossiblePaths = new string[]
    {
        "{0}", // Direct name
        "Assets/Graphics/Textures/{0}.png",
        "Assets/Graphics/Textures/{0}.jpg",
        "Assets/Graphics/Textures/{0}.tga",
        "Assets/Graphics/{0}.png",
        "Assets/Graphics/{0}.jpg",
        "Assets/Graphics/{0}.tga"
    };

    private const int PROGRESS_REPORT_INTERVAL = 5; // Report progress every N items
    private const int YIELD_INTERVAL = 5; // Yield every N items to prevent frame drops

    // State tracking
    private static bool _isWorking = false;
    private static MonoBehaviour _coroutineRunner;

    public static bool isWorking => _isWorking;

    public static void StartPreload(Scene scene, UnityAction<int> loadingProgress, int progressFrom, int progressTo)
    {
        if (_isWorking)
        {
            DebugHelper.LogWarning(null, "MaterialPreloader: Already working, ignoring new request.");
            return;
        }

        // Create a temporary GameObject to run the coroutine
        if (_coroutineRunner == null)
        {
            GameObject runnerObj = new GameObject("MaterialPreloader_Runner");
            _coroutineRunner = runnerObj.AddComponent<MaterialPreloaderRunner>();
            UnityEngine.Object.DontDestroyOnLoad(runnerObj);
        }

        _isWorking = true;
        _coroutineRunner.StartCoroutine(PreloadMaterialAssetsCoroutine(scene, loadingProgress, progressFrom, progressTo));
    }

    private static IEnumerator PreloadMaterialAssetsCoroutine(
        Scene scene,
        UnityAction<int> loadingProgress,
        int progressFrom,
        int progressTo)
    {
        try
        {
            // Stage 1: Find all materials and textures
            int stage1End = progressFrom + (progressTo - progressFrom) / 4;
            var materials = GetAllMaterialsInScene(scene);
            
            if (materials.Count == 0)
            {
                ReportProgress(loadingProgress, progressTo, "MaterialPreloader: Skipped (no materials found)");
                yield break;
            }
            
            var textures = ExtractTexturesFromMaterials(materials);
            ReportProgress(loadingProgress, stage1End, "Stage 1: Found {0} materials and {1} textures", materials.Count, textures.Count);

            // Stage 2: Check which are Addressables
            int stage2End = progressFrom + (progressTo - progressFrom) * 2 / 4;
            var materialHandles = CreateMaterialHandles(materials);
            var textureHandles = CreateTextureHandles(textures);
            var nonAddressableTextures = GetNonAddressableTextures(textures, textureHandles.Count);
            ReportProgress(loadingProgress, stage2End, "Stage 2: {0} materials and {1} textures are Addressables", 
                materialHandles.Count, textureHandles.Count);

            // Stage 3 & 4: Load materials and textures (can be interchanged)
            int stage3End = progressFrom + (progressTo - progressFrom) * 3 / 4;
            int stage4End = progressTo;

            // Load materials (Stage 3)
            if (materialHandles.Count > 0)
            {
                yield return LoadAssetsWithProgress(materialHandles, loadingProgress, stage2End, stage3End, "materials");
                ReleaseHandles(materialHandles);
            }
            else
            {
                ReportProgress(loadingProgress, stage3End, "Stage 3: No materials to load");
            }

            // Load textures (Stage 4)
            if (textureHandles.Count > 0 || nonAddressableTextures.Count > 0)
            {
                int textureProgressFrom = stage3End;
                int textureProgressTo = stage4End;

                if (textureHandles.Count > 0)
                {
                    int addressableProgressTo = textureHandles.Count > 0
                        ? textureProgressFrom + (textureProgressTo - textureProgressFrom) * textureHandles.Count / (textureHandles.Count + nonAddressableTextures.Count)
                        : textureProgressFrom;

                    yield return LoadAssetsWithProgress(textureHandles, loadingProgress, textureProgressFrom, addressableProgressTo, "textures");
                    ReleaseHandles(textureHandles);
                }

                if (nonAddressableTextures.Count > 0)
                {
                    int nonAddressableProgressFrom = textureHandles.Count > 0
                        ? textureProgressFrom + (textureProgressTo - textureProgressFrom) * textureHandles.Count / (textureHandles.Count + nonAddressableTextures.Count)
                        : textureProgressFrom;

                    yield return ForceLoadNonAddressableTextures(nonAddressableTextures, loadingProgress, nonAddressableProgressFrom, textureProgressTo);
                }
            }
            else
            {
                ReportProgress(loadingProgress, stage4End, "Stage 4: No textures to load");
            }

            ReportProgress(loadingProgress, progressTo, "MaterialPreloader: Completed");
        }
        finally
        {
            _isWorking = false;
        }
    }

    private static List<Material> GetAllMaterialsInScene(Scene scene)
    {
        var uniqueMaterials = new HashSet<Material>();

        foreach (var rootObject in scene.GetRootGameObjects())
        {
            var meshRenderers = rootObject.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var meshRenderer in meshRenderers)
            {
                foreach (var material in meshRenderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        uniqueMaterials.Add(material);
                    }
                }
            }
        }

        DebugHelper.LogWarning(null, $"MaterialPreloader: Found {uniqueMaterials.Count} unique materials in scene '{scene.name}'");
        return new List<Material>(uniqueMaterials);
    }

    private static HashSet<Texture> ExtractTexturesFromMaterials(List<Material> materials)
    {
        var uniqueTextures = new HashSet<Texture>();

        foreach (var material in materials)
        {
            if (material == null || material.shader == null)
                continue;

            Shader shader = material.shader;

            // Get all texture properties from the shader
            int propertyCount = shader.GetPropertyCount();
            for (int i = 0; i < propertyCount; i++)
            {
                if (shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture)
                {
                    string propertyName = shader.GetPropertyName(i);
                    Texture texture = material.GetTexture(propertyName);
                    if (texture != null)
                    {
                        uniqueTextures.Add(texture);
                    }
                }
            }

            // Also check common texture properties that might not be in shader
            foreach (string propName in CommonTextureProperties)
            {
                if (material.HasProperty(propName))
                {
                    Texture texture = material.GetTexture(propName);
                    if (texture != null)
                    {
                        uniqueTextures.Add(texture);
                    }
                }
            }
        }

        DebugHelper.Log(null, $"MaterialPreloader: Extracted {uniqueTextures.Count} unique textures from materials.");
        return uniqueTextures;
    }

    private static List<AsyncOperationHandle<Material>> CreateMaterialHandles(List<Material> materials)
    {
        var handles = new List<AsyncOperationHandle<Material>>();

        foreach (var material in materials)
        {
            if (material == null || string.IsNullOrEmpty(material.name))
                continue;

            string addressableKey = $"Assets/Graphics/Materials/{material.name}.mat";
            if (CheckResourceExists(addressableKey))
            {
                try
                {
                    var handle = Addressables.LoadAssetAsync<Material>(addressableKey);
                    handles.Add(handle);
                }
                catch (Exception ex)
                {
                    DebugHelper.LogWarning(null, $"MaterialPreloader: Failed to create load handle for '{material.name}'. {ex.Message}");
                }
            }
        }

        return handles;
    }

    private static List<AsyncOperationHandle<Texture>> CreateTextureHandles(HashSet<Texture> textures)
    {
        var handles = new List<AsyncOperationHandle<Texture>>();

        foreach (var texture in textures)
        {
            if (texture == null || string.IsNullOrEmpty(texture.name))
                continue;

            string addressableKey = FindAddressableKey(texture.name);
            if (addressableKey != null)
            {
                try
                {
                    var handle = Addressables.LoadAssetAsync<Texture>(addressableKey);
                    handles.Add(handle);
                }
                catch (Exception ex)
                {
                    DebugHelper.LogWarning(null, $"MaterialPreloader: Failed to create texture load handle for '{texture.name}'. {ex.Message}");
                }
            }
        }

        return handles;
    }

    private static HashSet<Texture> GetNonAddressableTextures(HashSet<Texture> allTextures, int addressableCount)
    {
        var nonAddressable = new HashSet<Texture>();
        int processed = 0;

        foreach (var texture in allTextures)
        {
            if (texture == null || string.IsNullOrEmpty(texture.name))
                continue;

            string addressableKey = FindAddressableKey(texture.name);
            if (addressableKey == null)
            {
                nonAddressable.Add(texture);
            }
            processed++;
        }

        return nonAddressable;
    }

    private static string FindAddressableKey(string textureName)
    {
        foreach (string pathTemplate in TexturePossiblePaths)
        {
            string addressableKey = string.Format(pathTemplate, textureName);
            if (CheckResourceExists(addressableKey))
            {
                return addressableKey;
            }
        }
        return null;
    }

    private static bool CheckResourceExists(string addressableKey)
    {
        AsyncOperationHandle<IList<IResourceLocation>> locateHandle = default;
        bool resourceExists = false;

        try
        {
            locateHandle = Addressables.LoadResourceLocationsAsync(addressableKey);
            locateHandle.WaitForCompletion();
            if (locateHandle.Status == AsyncOperationStatus.Succeeded &&
                locateHandle.Result != null &&
                locateHandle.Result.Count > 0)
            {
                resourceExists = true;
            }
        }
        catch (Exception)
        {
            resourceExists = false;
        }
        finally
        {
            if (locateHandle.IsValid())
            {
                Addressables.Release(locateHandle);
            }
        }

        return resourceExists;
    }

    private static IEnumerator LoadAssetsWithProgress<T>(
        List<AsyncOperationHandle<T>> handles,
        UnityAction<int> loadingProgress,
        int progressFrom,
        int progressTo,
        string assetType) where T : UnityEngine.Object
    {
        DebugHelper.Log(null, $"MaterialPreloader: Preloading {handles.Count} {assetType} from Addressables...");
        yield return null;

        int loadedCount = 0;
        int lastReportedProgress = progressFrom;

        foreach (var handle in handles)
        {
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Failed)
            {
                if (!(handle.OperationException is InvalidKeyException))
                {
                    Debug.LogWarning($"MaterialPreloader: Failed to load {assetType} from Addressables. Status: {handle.Status}, Error: {handle.OperationException?.Message}");
                }
            }

            loadedCount++;
            int currentProgress = CalculateProgress(loadedCount, handles.Count, progressFrom, progressTo);

            // Report progress only every N items
            if (loadedCount % PROGRESS_REPORT_INTERVAL == 0 || loadedCount == handles.Count)
            {
                loadingProgress?.Invoke(currentProgress);
                lastReportedProgress = currentProgress;
            }
        }

        // Ensure final progress is reported
        if (lastReportedProgress < progressTo)
        {
            loadingProgress?.Invoke(progressTo);
        }

        DebugHelper.Log(null, $"MaterialPreloader: Finished preloading {assetType} from Addressables.");
    }

    private static IEnumerator ForceLoadNonAddressableTextures(
        HashSet<Texture> textures,
        UnityAction<int> loadingProgress,
        int progressFrom,
        int progressTo)
    {
        DebugHelper.Log(null, $"MaterialPreloader: Force loading {textures.Count} non-Addressable textures...");
        yield return null;

        int loadedCount = 0;
        int lastReportedProgress = progressFrom;

        foreach (var texture in textures)
        {
            if (texture == null)
            {
                loadedCount++;
                continue;
            }

            try
            {
                int width = texture.width;
                int height = texture.height;

                if (texture is Texture2D tex2D)
                {
                    TextureFormat format = tex2D.format;
                    if (tex2D.width > 0 && tex2D.height > 0)
                    {
                        try
                        {
                            Color pixel = tex2D.GetPixelBilinear(0.5f, 0.5f);
                        }
                        catch
                        {
                            // Texture might not be readable, that's okay
                        }
                    }
                }
                else if (texture is RenderTexture rt)
                {
                    width = rt.width;
                    height = rt.height;
                }
            }
            catch (Exception ex)
            {
                DebugHelper.LogWarning(null, $"MaterialPreloader: Failed to force load texture '{texture.name}'. {ex.Message}");
            }

            loadedCount++;
            int currentProgress = CalculateProgress(loadedCount, textures.Count, progressFrom, progressTo);

            // Report progress only every N items
            if (loadedCount % PROGRESS_REPORT_INTERVAL == 0 || loadedCount == textures.Count)
            {
                loadingProgress?.Invoke(currentProgress);
                lastReportedProgress = currentProgress;
            }

            // Yield every few textures to prevent frame drops
            if (loadedCount % YIELD_INTERVAL == 0)
            {
                yield return null;
            }
        }

        // Ensure final progress is reported
        if (lastReportedProgress < progressTo)
        {
            loadingProgress?.Invoke(progressTo);
        }

        DebugHelper.Log(null, "MaterialPreloader: Finished force loading non-Addressable textures.");
    }

    private static void ReleaseHandles<T>(List<AsyncOperationHandle<T>> handles) where T : UnityEngine.Object
    {
        foreach (var handle in handles)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
    }

    private static int CalculateProgress(int current, int total, int from, int to)
    {
        return current.Map(0, total, from, to);
    }

    private static void ReportProgress(UnityAction<int> loadingProgress, int progress, string message, params object[] args)
    {
        if (args.Length > 0)
        {
            DebugHelper.Log(null, string.Format($"MaterialPreloader: {message}", args));
        }
        else
        {
            DebugHelper.Log(null, $"MaterialPreloader: {message}");
        }
        loadingProgress?.Invoke(progress);
    }
}

// Helper MonoBehaviour to run coroutines
internal class MaterialPreloaderRunner : MonoBehaviour { }

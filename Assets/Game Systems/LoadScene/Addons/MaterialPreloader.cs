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
using static UnityEngine.Rendering.DebugUI;

public static class MaterialPreloader
{
    public static IEnumerator PreloadMaterialAssets(
        Scene scene, 
        UnityAction<int> loadingProgress, 
        int progressFrom, 
        int progressTo)
    {
        var materials = GetAllMaterialsInScene(scene);
        var handles = CreateMaterialHandles(materials);

        if (handles.Count == 0)
        {
            loadingProgress?.Invoke(progressTo);
            DebugHelper.LogWarning(null, "MaterialPreloader: No materials to preload from Addressables.");
            yield break;
        }

        yield return LoadMaterialsWithProgress(handles, loadingProgress, progressFrom, progressTo);
        ReleaseMaterialHandles(handles);
        
        yield return null;
        loadingProgress?.Invoke(progressTo);
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

    private static List<AsyncOperationHandle<Material>> CreateMaterialHandles(List<Material> materials)
    {
        var handles = new List<AsyncOperationHandle<Material>>();
        
        foreach (var material in materials)
        {
            if (!IsMaterialValidForPreload(material))
            {
                continue;
            }

            var handle = TryCreateMaterialHandle(material);
            if (handle.HasValue)
            {
                handles.Add(handle.Value);
            }
        }

        return handles;
    }

    private static bool IsMaterialValidForPreload(Material material)
    {
        return material != null && !string.IsNullOrEmpty(material.name);
    }

    private static AsyncOperationHandle<Material>? TryCreateMaterialHandle(Material material)
    {
        string addressableKey = $"Assets/Graphics/Materials/{material.name}.mat";
        
        // Try to locate the resource first to check if it exists
        // This prevents InvalidKeyException from being logged to console
        AsyncOperationHandle<IList<IResourceLocation>> locateHandle = default;
        bool resourceExists = false;
        
        try
        {
            locateHandle = Addressables.LoadResourceLocationsAsync(addressableKey);
            locateHandle.WaitForCompletion();
            if (locateHandle.Status == AsyncOperationStatus.Succeeded && locateHandle.Result != null && locateHandle.Result.Count > 0)
                resourceExists = true;
            else
                resourceExists = false;
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
        
        if (!resourceExists)
        {
            return null;
        }
        
        try
        {
            var materialHandle = Addressables.LoadAssetAsync<Material>(addressableKey);
            return materialHandle;
        }
        catch (Exception ex)
        {
            DebugHelper.LogWarning(null, $"MaterialPreloader: Failed to create load handle for '{material.name}'. {ex.Message}");
            return null;
        }
    }

    private static IEnumerator LoadMaterialsWithProgress(
        List<AsyncOperationHandle<Material>> handles, 
        UnityAction<int> loadingProgress, 
        int progressFrom, 
        int progressTo)
    {
        DebugHelper.Log(null, $"MaterialPreloader: Preloading {handles.Count} materials from Addressables...");
        yield return null;
        loadingProgress?.Invoke(progressFrom);

        int loadedCount = 0;
        foreach (var handle in handles)
        {
            yield return handle;
            
            // Check handle status and handle errors silently
            if (handle.Status == AsyncOperationStatus.Failed)
            {
                // Check if it's an InvalidKeyException and handle it silently
                if (handle.OperationException is InvalidKeyException)
                {
                    // Silently skip - we already checked with ResourceExists, but handle edge cases
                    DebugHelper.LogWarning(null, $"MaterialPreloader: Material key not found (edge case), skipping.");
                }
                else
                {
                    Debug.LogWarning($"MaterialPreloader: Failed to load material from Addressables. Status: {handle.Status}, Error: {handle.OperationException?.Message}");
                }
            }
            else if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // Successfully loaded - material is now in cache
            }

            loadedCount++;
            int currentProgress = CalculateProgress(loadedCount, handles.Count, progressFrom, progressTo);
            loadingProgress?.Invoke(currentProgress);
        }

        DebugHelper.Log(null, "MaterialPreloader: Finished preloading materials from Addressables.");
    }

    private static int CalculateProgress(int current, int total, int from, int to)
    {
        return current.Map(0, total, from, to);
    }

    private static void ReleaseMaterialHandles(List<AsyncOperationHandle<Material>> handles)
    {
        foreach (var handle in handles)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        DebugHelper.Log(null, "MaterialPreloader: Released Addressables material handles.");
    }
}


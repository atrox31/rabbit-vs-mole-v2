using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Manages a synchronous cache of GameObjects loaded from Unity Addressables.
/// </summary>
public class AddressablesStaticDictionary<T> : IDisposable
{
    private readonly string _pathToAddressablePre;
    private readonly string _pathToAddressablePost;
    private bool _isDisposed = false;

    // Dictionary to cache the loaded prefabs.
    private readonly Dictionary<T, GameObject> _prefabsDictionary = new Dictionary<T, GameObject>();

    // Dictionary to store the original AsyncOperationHandles required for safe release
    private readonly Dictionary<T, AsyncOperationHandle<GameObject>> _handleDictionary = new Dictionary<T, AsyncOperationHandle<GameObject>>();

    private string GetObjectPrefabPath(T type) => string.Concat(_pathToAddressablePre, type.ToString(), _pathToAddressablePost);

    public AddressablesStaticDictionary(string pathToAddressablePre, string pathToAddressablePost)
    {
        _pathToAddressablePre = pathToAddressablePre;
        _pathToAddressablePost = pathToAddressablePost;
    }

    /// <summary>
    /// Synchronously loads the prefab from Addressables and caches it for subsequent calls.
    /// This method blocks the execution until the resource is loaded.
    /// </summary>
    /// <param name="type">The key representing the asset to load.</param>
    /// <returns>The loaded GameObject prefab, or null if loading failed.</returns>
    public GameObject GetPrefab(T type)
    {
        // 1. Check if the asset is already in the cache.
        if (_prefabsDictionary.TryGetValue(type, out GameObject cachedPrefab))
        {
            return cachedPrefab;
        }

        // 2. Load the asset synchronously.
        string path = GetObjectPrefabPath(type);
        AsyncOperationHandle<GameObject> prefabHandle = Addressables.LoadAssetAsync<GameObject>(path);
        GameObject prefab = prefabHandle.WaitForCompletion();

        // 3. Error handling.
        if (prefab == null)
        {
            Debug.LogError($"AddressablesDictionary: Could not load prefab for type {type} at path {path}. Operation status: {prefabHandle.Status}");
            if (prefabHandle.IsValid())
            {
                Addressables.Release(prefabHandle);
            }
            return null;
        }

        // 4. Cache the loaded asset and its handle.
        _prefabsDictionary.Add(type, prefab);
        _handleDictionary.Add(type, prefabHandle);

        return prefab;
    }

    /// <summary>
    /// Releases all cached Addressables assets deterministically.
    /// </summary>
    public void Dispose()
    {
        // Check to prevent double disposing.
        if (_isDisposed)
            return;

        _isDisposed = true;

        if (_handleDictionary.Count == 0)
            return;

        foreach (var handle in _handleDictionary.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        _prefabsDictionary.Clear();
        _handleDictionary.Clear();

        GC.SuppressFinalize(this);
    }
}
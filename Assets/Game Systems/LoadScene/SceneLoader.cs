using EasyTransition;
using Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // singleton
    private static SceneLoader _instance;

    // settings
    [SerializeField] TransitionSettings transitionSettings;
    [SerializeField] private string LOADING_SCENE_NAME = "Loading";

    // transition variables
    private bool _isTransitioning = false;
    private bool _blocker = false;
    private Scene _previousScene;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            DestroyImmediate(gameObject);
            return;
        }

        if (transitionSettings == null)
        {
            Debug.LogError("SceneLoader: Awake error! Transition Settings not assigned in SceneLoader.");
            DestroyImmediate(gameObject);
            return;
        }
    }

    public static void ChangeScene(string targetSceneName, UnityAction<Scene> onNewSceneLoad, UnityAction onNewSceneStart, UnityAction<int> loadingProgress)
    {
        if (_instance == null)
        {
            Debug.LogError("SceneLoader: ChangeScene called but SceneLoader instance is null! Make sure SceneLoader exists in the scene.");
            return;
        }

        if (_instance._isTransitioning)
        {
            Debug.LogWarning("Scene transition already in progress. Ignoring ChangeScene call.");
            return;
        }

        _instance.StartCoroutine(_instance.LoadSceneWithLoadingScreen(targetSceneName, onNewSceneLoad, onNewSceneStart, loadingProgress));
    }

    private IEnumerator LoadSceneWithLoadingScreen(string targetSceneName, UnityAction<Scene> onNewSceneLoad, UnityAction onNewSceneStart, UnityAction<int> loadingProgress)
    {
        _isTransitioning = true;

        try
        {
            // Store reference to current scene (excluding Loading scene)
            Scene currentActiveScene = SceneManager.GetActiveScene();
            if (currentActiveScene.name != LOADING_SCENE_NAME)
            {
                _previousScene = currentActiveScene;
            }

            loadingProgress?.Invoke(0);
            Debug.Log("LoadSceneWithLoadingScreen: StartCoroutine(ShowLoadingScreen())");
            yield return StartCoroutine(ShowLoadingScreen());    
            
            yield return null;

            Debug.Log("LoadSceneWithLoadingScreen: StartCoroutine(LoadNewScene(loadingProgress, onNewSceneLoad))");
            yield return StartCoroutine(LoadNewScene(targetSceneName, loadingProgress, onNewSceneLoad, onNewSceneStart)); 

            yield return null;
        }
        finally
        {
            // Always reset transition state, even if error occurred
            _isTransitioning = false;
            Time.timeScale = 1.0f;
        }
    }

    private IEnumerator ShowLoadingScreen()
    {
        CreateTransitionManager();
        Scene currentScene = SceneManager.GetActiveScene();
        Scene loadingScene = SceneManager.GetSceneByName(LOADING_SCENE_NAME);

        if (currentScene.name == LOADING_SCENE_NAME)
        {
            if (loadingScene.IsValid())
            {
                SceneManager.SetActiveScene(loadingScene);
            }
            yield break;
        }

        var transitionManager = TransitionManager.Instance();
        if (transitionManager == null)
        {
            Debug.LogError("SceneLoader: TransitionManager.Instance() returned null!");
            yield break;
        }

        _blocker = true;
        transitionManager.onTransitionEnd += UnLockProgress;
        transitionManager.Transition(LOADING_SCENE_NAME, transitionSettings, 0.0f);

        while(_blocker)
            yield return null;

        transitionManager.onTransitionEnd -= UnLockProgress;
    }


    private IEnumerator LoadNewScene(string targetSceneName, UnityAction<int> loadingProgress, UnityAction<Scene> onNewSceneLoad, UnityAction onNewSceneStart)
    {
        // Check if scene already exists
        var existingScene = SceneManager.GetSceneByName(targetSceneName);
        if (existingScene.IsValid() && existingScene.isLoaded)
        {
            Debug.LogWarning($"SceneLoader: Scene '{targetSceneName}' is already loaded!");
            onNewSceneLoad?.Invoke(existingScene);
            onNewSceneStart?.Invoke();
            yield break;
        }

        var newSceneProcess = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
        if (newSceneProcess == null)
        {
            Debug.LogError($"SceneLoader: Failed to start loading scene '{targetSceneName}'. Scene may not exist in build settings.");
            yield break;
        }
        
        newSceneProcess.allowSceneActivation = false;

        // Wait until target scene loads (up to 90%)
        while (newSceneProcess.progress < 0.9f)
        {
            loadingProgress?.Invoke((int)newSceneProcess.progress.Map(.0f, 90.0f, .0f, 50.0f));

            yield return null;
        }

        loadingProgress?.Invoke(50);
        yield return null;

        var newSceneObject = SceneManager.GetSceneByName(targetSceneName);
        if (!newSceneObject.IsValid())
        {
            DebugHelper.LogError(this, $"Scene '{targetSceneName}' is not valid!");
            yield break;
        }
        
        onNewSceneLoad?.Invoke(newSceneObject);

        loadingProgress?.Invoke(55);
        yield return null;

        yield return StartCoroutine(
            PreloadMaterialAssets(
                newSceneObject,
                loadingProgress,
                55, 90));

        yield return null;
        loadingProgress?.Invoke(90);

        
        var transitionManager = TransitionManager.Instance();
        if (transitionManager == null)
        {
            Debug.LogError("SceneLoader: TransitionManager.Instance() returned null!");
            yield break;
        }

        transitionManager.onTransitionCutPointReached += UnLockProgress;
        transitionManager.onTransitionEnd += UnloadLoadingSceneCoroutine;
        transitionManager.Transition(transitionSettings, 0.0f);

        _blocker = true;
        while (_blocker)
            yield return null;
        transitionManager.onTransitionCutPointReached -= UnLockProgress;

        // Set time scale to 0 to prevent any updates during scene activation
        Time.timeScale = 0.0f;

        // Activate the scene
        newSceneProcess.allowSceneActivation = true;

        // Wait for scene to fully load (Awake and OnEnable will be called)
        while (!newSceneProcess.isDone)
        {
            yield return null;
        }

        // Wait one more frame to ensure all Awake/OnEnable methods have completed
        yield return null;

        loadingProgress?.Invoke(100);   // all ready

        yield return null;
        SceneManager.SetActiveScene(newSceneObject);

        yield return null;
        onNewSceneStart?.Invoke();

        // Reset time scale after scene is ready
        Time.timeScale = 1.0f;

        // Unload previous scene if it exists and is not the Loading scene
        if (_previousScene.IsValid() && _previousScene.name != LOADING_SCENE_NAME)
        {
            yield return SceneManager.UnloadSceneAsync(_previousScene);
        }
    }

    void UnloadLoadingSceneCoroutine()
    {
        StartCoroutine(UnloadLoadingSceneCoroutineInternal());
    }

    IEnumerator UnloadLoadingSceneCoroutineInternal()
    {
        // Unsubscribe from event first to avoid multiple calls
        var transitionManager = TransitionManager.Instance();
        if (transitionManager != null)
        {
            transitionManager.onTransitionEnd -= UnloadLoadingSceneCoroutine;
        }

        if (transitionManager != null)
        {
            Destroy(transitionManager.gameObject);
        }

        var loadScreenScene = SceneManager.GetSceneByName(LOADING_SCENE_NAME);
        if (!loadScreenScene.IsValid())
        {
            DebugHelper.LogError(this, $"Scene '{LOADING_SCENE_NAME}' is not valid!");
            yield break;
        }
        
        yield return SceneManager.UnloadSceneAsync(loadScreenScene);
    }

    private void CreateTransitionManager()
    {
        // Check if TransitionManager already exists
        var existingManager = TransitionManager.Instance();
        if (existingManager != null)
        {
            return;
        }

        var go = new GameObject("TransitionManager");
        var transitionManager = go.AddComponent<TransitionManager>(); 
        
        var handle = Addressables.LoadAssetAsync<GameObject>("Assets/Game Systems/EasyTransitions/Prefabs/TransitionTemplate.prefab");
        var prefab = handle.WaitForCompletion();
        if (prefab == null)
        {
            Debug.LogError("SceneLoader: Failed to load TransitionTemplate prefab from Addressables!");
            Destroy(go);
            return;
        }
        transitionManager.transitionTemplate = prefab;

        DontDestroyOnLoad(go);
    }
    private void UnLockProgress()
    {
        _blocker = false;
    }

    List<Material> GetAllMaterialsInScene(Scene scene)
    {
        var uniqueMaterials = new HashSet<Material>();
        foreach (var gameObject in GetAllGameObjectsInScene(scene))
        {
            if(gameObject.TryGetComponent(out MeshRenderer meshRenderer))
            {
                foreach (var material in meshRenderer.sharedMaterials)
                {
                    if (material != null)
                        uniqueMaterials.Add(material);
                }
            }
        }
        return new List<Material>(uniqueMaterials);
    }

    List<GameObject> GetAllGameObjectsInScene(Scene scene)
    {
        var gameObjectSet = new HashSet<GameObject>(); // Use HashSet to avoid duplicates

        foreach (var rootObject in scene.GetRootGameObjects())
        {
            // GetComponentsInChildren includes the root object itself
            Transform[] transforms = rootObject.GetComponentsInChildren<Transform>(true);

            foreach (var transform in transforms)
            {
                gameObjectSet.Add(transform.gameObject);
            }
        }

        return new List<GameObject>(gameObjectSet);
    }


    IEnumerator PreloadMaterialAssets(Scene scene, UnityAction<int> loadingProgress, int progressFrom, int progressTo)
    {
        List<AsyncOperationHandle<Material>> handlesToWaitFor = new List<AsyncOperationHandle<Material>>();
        
        // Try to preload materials from Addressables (only if they exist there)
        foreach (var material in GetAllMaterialsInScene(scene))
        {
            if (material != null && !string.IsNullOrEmpty(material.name))
            {
                try
                {
                    // Check if material exists in Addressables before trying to load
                    var handle = Addressables.LoadAssetAsync<Material>(material.name);
                    handlesToWaitFor.Add(handle);
                }
                catch (Exception ex)
                {
                    // Material not in Addressables, skip it
                    Debug.LogWarning($"SceneLoader: Material '{material.name}' not found in Addressables, skipping preload. {ex.Message}");
                }
            }
        }

        var itemsCount = handlesToWaitFor.Count;
        if (itemsCount == 0)
        {
            // No materials to load from Addressables, skip to end
            loadingProgress?.Invoke(progressTo);
            yield break;
        }

        yield return null;
        // Start at the beginning of the range
        loadingProgress?.Invoke(progressFrom);

        var counter = 0;
        foreach (var handle in handlesToWaitFor)
        {
            while (!handle.IsDone)
            {
                yield return null;
            }

            // Check if load was successful
            if (handle.Status == AsyncOperationStatus.Failed)
            {
                Debug.LogWarning($"SceneLoader: Failed to load material from Addressables. Status: {handle.Status}");
            }

            counter++;

            // Calculate progress: 0.0 to 1.0 based on loaded items
            float progressPercent = (float)counter / (float)itemsCount;
            // Map 0.0-1.0 to progressFrom-progressTo range
            int currentProgress = progressFrom + (int)((progressTo - progressFrom) * progressPercent);
            loadingProgress?.Invoke(currentProgress);
        }

        // Release handles to prevent memory leaks
        foreach (var handle in handlesToWaitFor)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        yield return null;
        loadingProgress?.Invoke(progressTo);
    }
}
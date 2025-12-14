using EasyTransition;
using Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public class SceneLoader : SingletonMonoBehaviour<SceneLoader>
{
    private const string LOADING_SCENE_NAME = "Loading";
    private const string TRANSITION_TEMPLATE_PATH = "Assets/Game Systems/EasyTransitions/Prefabs/TransitionTemplate.prefab";
    
    // Progress constants
    private const int PROGRESS_SCENE_LOADED = 50;
    private const int PROGRESS_MATERIALS_START = 75;
    private const int PROGRESS_MATERIALS_END = 99;
    private const int PROGRESS_COMPLETE = 100;
    private const float SCENE_ACTIVATION_DELAY = 1.0f;
    private const float SCENE_LOAD_PROGRESS_THRESHOLD = 0.9f;

    [SerializeField] private TransitionSettings transitionSettings;

    private bool _isTransitioning = false;
    private Scene _previousScene;
    private Scene _targetScene;
    private AsyncOperation _targetSceneAsyncOperation;
    private bool _isWaitingForTransition = false;

    private Action OnSceneUnload;

    private void SetProgressBlocked(bool blocked)
    {
        _isWaitingForTransition = blocked;
    }

    private IEnumerator WaitForTransition()
    {
        while (_isWaitingForTransition)
        {
            yield return null;
        }
    }

    protected override void Ready()
    {
        if (transitionSettings == null)
        {
            Debug.LogError("SceneLoader: Transition Settings not assigned in SceneLoader.");
            DestroyImmediate(gameObject);
        }
    }

    public static void SetOnSceneUnload(Action onSceneUnload)
    {
        if (Instance == null)
        {
            Debug.LogError("SceneLoader: SetOnSceneUnload error! SceneLoader instance is null. Make sure SceneLoader exists in the initial scene.");
            return;
        }
        Instance.OnSceneUnload = onSceneUnload;
    }

    public override void OnGameStart() { }

    public static void ChangeScene(
        string targetSceneName, 
        UnityAction<Scene> onNewSceneLoad, 
        UnityAction onNewSceneStart,
        UnityAction onNewSceneShow, 
        UnityAction<int> loadingProgress)
    {
        if (Instance == null)
        {
            Debug.LogError("SceneLoader: ChangeScene error! SceneLoader instance is null. Make sure SceneLoader exists in the initial scene.");
            return;
        }

        if (Instance._isTransitioning)
        {
            Debug.LogWarning("Scene transition already in progress. Ignoring ChangeScene call.");
            return;
        }

        Instance.StartCoroutine(Instance.LoadSceneWithLoadingScreen(
            targetSceneName, onNewSceneLoad, onNewSceneStart, onNewSceneShow, loadingProgress));
    }

    private IEnumerator LoadSceneWithLoadingScreen(
        string targetSceneName, 
        UnityAction<Scene> onNewSceneLoad, 
        UnityAction onNewSceneStart, 
        UnityAction onNewSceneShow,
        UnityAction<int> loadingProgress)
    {
        _isTransitioning = true;

        try
        {
            if (targetSceneName == LOADING_SCENE_NAME)
            {
                Debug.LogError("SceneLoader: Target scene name cannot be the same as Loading scene name.");
                throw new Exception("Target scene name cannot be the same as Loading scene name.");
            }

            _previousScene = SceneManager.GetActiveScene();
            _targetSceneAsyncOperation = null;

            loadingProgress?.Invoke(0);
            yield return StartCoroutine(ShowLoadingScreen());
            yield return StartCoroutine(LoadNewScene(targetSceneName, loadingProgress, onNewSceneLoad));
            yield return StartCoroutine(ActivateNewScene(onNewSceneStart, onNewSceneShow));
        }
        finally
        {
            _isTransitioning = false;
            Time.timeScale = 1.0f;
        }
    }

    private IEnumerator ShowLoadingScreen()
    {
        CreateTransitionManager();
        Scene currentScene = SceneManager.GetActiveScene();
        Scene loadingScene = SceneManager.GetSceneByName(LOADING_SCENE_NAME);

        if (currentScene.name == LOADING_SCENE_NAME && loadingScene.IsValid())
        {
            SceneManager.SetActiveScene(loadingScene);
            yield break;
        }

        var transitionManager = GetTransitionManager();
        if (transitionManager == null)
        {
            yield break;
        }

        SetProgressBlocked(true);
        transitionManager.onTransitionEnd += OnLoadingTransitionEnd;
        transitionManager.Transition(LOADING_SCENE_NAME, transitionSettings, 0.0f);
        
        yield return null;
        yield return StartCoroutine(WaitForTransition());
        OnSceneUnload?.Invoke();
    }

    private void OnLoadingTransitionEnd()
    {
        var transitionManager = GetTransitionManager();
        if (transitionManager != null)
        {
            transitionManager.onTransitionEnd -= OnLoadingTransitionEnd;
        }
        SetProgressBlocked(false);
    }


    private IEnumerator LoadNewScene(string targetSceneName, UnityAction<int> loadingProgress, UnityAction<Scene> onNewSceneLoad)
    {
        if (_previousScene.IsValid() && _previousScene.name != LOADING_SCENE_NAME)
        {
            yield return SceneManager.UnloadSceneAsync(_previousScene);
        }

        var existingScene = SceneManager.GetSceneByName(targetSceneName);
        if (existingScene.IsValid())
        {
            Debug.LogWarning($"SceneLoader: Scene '{targetSceneName}' is already loaded!");
            yield break;
        }

        var newSceneProcess = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
        if (newSceneProcess == null)
        {
            Debug.LogError($"SceneLoader: Failed to start loading scene '{targetSceneName}'. Scene may not exist in build settings.");
            yield break;
        }

        _targetSceneAsyncOperation = newSceneProcess;
        newSceneProcess.allowSceneActivation = false;

        while (newSceneProcess.progress < SCENE_LOAD_PROGRESS_THRESHOLD)
        {
            int progress = (int)newSceneProcess.progress.Map(0.0f, 90.0f, 0.0f, PROGRESS_SCENE_LOADED);
            loadingProgress?.Invoke(progress);
            yield return null;
        }

        loadingProgress?.Invoke(PROGRESS_SCENE_LOADED);
        yield return null;

        var newSceneObject = SceneManager.GetSceneByName(targetSceneName);
        if (!ValidateScene(newSceneObject, targetSceneName))
        {
            yield break;
        }

        onNewSceneLoad?.Invoke(newSceneObject);
        loadingProgress?.Invoke(PROGRESS_MATERIALS_START);
        yield return null;

        yield return StartCoroutine(PreloadMaterialAssets(
            newSceneObject, loadingProgress, PROGRESS_MATERIALS_START, PROGRESS_MATERIALS_END));

        yield return null;
        loadingProgress?.Invoke(PROGRESS_COMPLETE);
        _targetScene = newSceneObject;
    }

    private IEnumerator ActivateNewScene(UnityAction onNewSceneStart, UnityAction onNewSceneShow)
    {
        if (!ValidateScene(_targetScene, "target scene"))
        {
            yield break;
        }

        Time.timeScale = 0.0f;
        _targetSceneAsyncOperation.allowSceneActivation = true;

        while (!_targetSceneAsyncOperation.isDone)
        {
            yield return null;
        }

        SceneManager.SetActiveScene(_targetScene);
        yield return null;
        onNewSceneStart?.Invoke();

        yield return new WaitForSecondsRealtime(SCENE_ACTIVATION_DELAY);
        Time.timeScale = 1.0f;

        var transitionManager = GetTransitionManager();
        if (transitionManager == null)
        {
            yield break;
        }

        SetProgressBlocked(true);
        transitionManager.onTransitionCutPointReached += OnTransitionCutPointReached;
        transitionManager.onTransitionEnd += OnTransitionEnd;
        transitionManager.Transition(transitionSettings, 0.0f);

        yield return null;
        yield return StartCoroutine(WaitForTransition());
        onNewSceneShow?.Invoke();
    }

    private void OnTransitionCutPointReached()
    {
        var transitionManager = GetTransitionManager();
        if (transitionManager != null)
        {
            transitionManager.onTransitionCutPointReached -= OnTransitionCutPointReached;
        }
        StartCoroutine(DestroyLoadingScene());
    }

    private void OnTransitionEnd()
    {
        SetProgressBlocked(false);
        var transitionManager = GetTransitionManager();
        if (transitionManager != null)
        {
            transitionManager.onTransitionEnd -= OnTransitionEnd;
            Destroy(transitionManager.gameObject);
        }
    }
    private IEnumerator DestroyLoadingScene()
    {
        var loadScreenScene = SceneManager.GetSceneByName(LOADING_SCENE_NAME);
        if (!loadScreenScene.IsValid())
        {
            DebugHelper.LogError(null, $"Scene '{LOADING_SCENE_NAME}' is not valid!");
            yield break;
        }
        yield return SceneManager.UnloadSceneAsync(loadScreenScene);
    }

    private void CreateTransitionManager()
    {
        if (TransitionManager.IsInstanceAvailable())
        {
            return;
        }

        var go = new GameObject("TransitionManager");
        var transitionManager = go.AddComponent<TransitionManager>();
        
        var handle = Addressables.LoadAssetAsync<GameObject>(TRANSITION_TEMPLATE_PATH);
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

    private TransitionManager GetTransitionManager()
    {
        var transitionManager = TransitionManager.Instance();
        if (transitionManager == null)
        {
            Debug.LogError("SceneLoader: TransitionManager.Instance() returned null!");
        }
        return transitionManager;
    }

    private bool ValidateScene(Scene scene, string sceneName)
    {
        if (!scene.IsValid())
        {
            Debug.LogError($"Scene '{sceneName}' is not valid!");
            return false;
        }
        return true;
    }
  
    private List<Material> GetAllMaterialsInScene(Scene scene)
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

        DebugHelper.LogWarning(this, $"SceneLoader: Found {uniqueMaterials.Count} unique materials in scene '{scene.name}'");
        return new List<Material>(uniqueMaterials);
    }


    private IEnumerator PreloadMaterialAssets(Scene scene, UnityAction<int> loadingProgress, int progressFrom, int progressTo)
    {
        var handlesToWaitFor = new List<AsyncOperationHandle<Material>>();
        
        foreach (var material in GetAllMaterialsInScene(scene))
        {
            if (material != null && !string.IsNullOrEmpty(material.name))
            {
                try
                {
                    var handle = Addressables.LoadAssetAsync<Material>(material.name);
                    handlesToWaitFor.Add(handle);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"SceneLoader: Material '{material.name}' not found in Addressables, skipping preload. {ex.Message}");
                }
            }
        }

        if (handlesToWaitFor.Count == 0)
        {
            loadingProgress?.Invoke(progressTo);
            DebugHelper.LogWarning(this, "SceneLoader: No materials to preload from Addressables.");
            yield break;
        }

        DebugHelper.Log(this, $"SceneLoader: Preloading {handlesToWaitFor.Count} materials from Addressables...");
        yield return null;
        loadingProgress?.Invoke(progressFrom);

        int counter = 0;
        foreach (var handle in handlesToWaitFor)
        {
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Failed)
            {
                Debug.LogWarning($"SceneLoader: Failed to load material from Addressables. Status: {handle.Status}");
            }

            counter++;
            float progressPercent = (float)counter / handlesToWaitFor.Count;
            int currentProgress = progressFrom + (int)((progressTo - progressFrom) * progressPercent);
            loadingProgress?.Invoke(currentProgress);
        }

        DebugHelper.Log(this, "SceneLoader: Finished preloading materials from Addressables.");

        foreach (var handle in handlesToWaitFor)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        DebugHelper.Log(this, "SceneLoader: Released Addressables material handles.");
        yield return null;
        loadingProgress?.Invoke(progressTo);
    }
}
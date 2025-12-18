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
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneLoader : SingletonMonoBehaviour<SceneLoader>
{
    private const string LOADING_SCENE_NAME = "Loading";
    private const string TRANSITION_TEMPLATE_PATH = "Assets/Game Systems/EasyTransitions/Prefabs/TransitionTemplate.prefab";
#if UNITY_EDITOR
    private const string ADDRESSABLES_SCENE_PATH_TEMPLATE = "Assets/Scenes/Test/{0}.unity";
#endif
    
    // Progress constants
    private const int PROGRESS_SCENE_DESERIALIZED = 50;
    private const int PROGRESS_SCENE_LOAD = 60;
    private const int PROGRESS_SCENE_ACTIVATION = 70;
    private const int PROGRESS_MATERIALS_START = 71;
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
#if UNITY_EDITOR
    private AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>? _currentSceneAddressablesHandle;
    private bool _currentSceneWasFromAddressables = false;
#endif

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
            yield return StartCoroutine(ActivateNewScene(loadingProgress, onNewSceneStart, onNewSceneShow));
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
        // Release previous Addressables handle if needed (from the scene we're leaving)
#if UNITY_EDITOR
        if (_currentSceneWasFromAddressables && _currentSceneAddressablesHandle.HasValue)
        {
            Addressables.Release(_currentSceneAddressablesHandle.Value);
            _currentSceneAddressablesHandle = null;
            _currentSceneWasFromAddressables = false;
        }
#endif

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

#if UNITY_EDITOR
        // Check if scene exists in build settings first
        bool sceneInBuildSettings = IsSceneInBuildSettings(targetSceneName);
        
        if (!sceneInBuildSettings)
        {
            // Scene not in build settings, try loading from Addressables
            string addressablesPath = string.Format(ADDRESSABLES_SCENE_PATH_TEMPLATE, targetSceneName);
            Debug.Log($"SceneLoader: Scene '{targetSceneName}' not found in build settings. Attempting to load from Addressables: {addressablesPath}");

            var addressablesHandle = Addressables.LoadSceneAsync(addressablesPath, LoadSceneMode.Additive);
            
            if (addressablesHandle.IsValid())
            {
                while (!addressablesHandle.IsDone)
                {
                    float progress = addressablesHandle.PercentComplete;
                    int progressInt = (int)(progress * PROGRESS_SCENE_DESERIALIZED);
                    loadingProgress?.Invoke(progressInt);
                    yield return null;
                }

                if (addressablesHandle.Status == AsyncOperationStatus.Failed)
                {
                    Debug.LogError($"SceneLoader: Failed to load scene '{targetSceneName}' from Addressables at path '{addressablesPath}'. Error: {addressablesHandle.OperationException}");
                    Addressables.Release(addressablesHandle);
                    yield break;
                }

                var sceneInstance = addressablesHandle.Result;
                var addressablesScene = sceneInstance.Scene;
                
                if (!ValidateScene(addressablesScene, targetSceneName))
                {
                    Addressables.Release(addressablesHandle);
                    yield break;
                }

                // Store handle for current scene (will be released on next scene change)
                _currentSceneAddressablesHandle = addressablesHandle;
                _currentSceneWasFromAddressables = true;

                // Create a dummy AsyncOperation for compatibility with existing code
                _targetSceneAsyncOperation = new DummyAsyncOperation();
                loadingProgress?.Invoke(PROGRESS_SCENE_DESERIALIZED);
                yield return null;

                onNewSceneLoad?.Invoke(addressablesScene);
                yield return null;

                loadingProgress?.Invoke(PROGRESS_SCENE_LOAD);
                _targetScene = addressablesScene;
                yield break;
            }
            else
            {
                Debug.LogError($"SceneLoader: Failed to start loading scene '{targetSceneName}' from Addressables. Scene may not exist at path '{addressablesPath}'.");
                yield break;
            }
        }
#endif

        // Load from build settings (normal path)
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
            int progress = (int)newSceneProcess.progress.Map(0.0f, 90.0f, 0.0f, PROGRESS_SCENE_DESERIALIZED);
            loadingProgress?.Invoke(progress);
            yield return null;
        }

        loadingProgress?.Invoke(PROGRESS_SCENE_DESERIALIZED);
        yield return null;

        var newSceneObject = SceneManager.GetSceneByName(targetSceneName);
        if (!ValidateScene(newSceneObject, targetSceneName))
        {
            yield break;
        }

        onNewSceneLoad?.Invoke(newSceneObject);
        yield return null;

        loadingProgress?.Invoke(PROGRESS_SCENE_LOAD);
        _targetScene = newSceneObject;
    }

    private IEnumerator ActivateNewScene(UnityAction<int> loadingProgress, UnityAction onNewSceneStart, UnityAction onNewSceneShow)
    {
        if (!ValidateScene(_targetScene, "target scene"))
        {
            yield break;
        }

        Time.timeScale = 0.0f;
        
        // Check if scene was loaded from Addressables (DummyAsyncOperation means it's already activated)
        bool isAddressablesScene = _targetSceneAsyncOperation is DummyAsyncOperation;
        
        if (!isAddressablesScene)
        {
            _targetSceneAsyncOperation.allowSceneActivation = true;

            while (!_targetSceneAsyncOperation.isDone)
            {
                yield return null;
            }
        }
        
        loadingProgress?.Invoke(PROGRESS_SCENE_ACTIVATION);

        MaterialPreloader.StartPreload(_targetScene, loadingProgress, PROGRESS_MATERIALS_START, PROGRESS_MATERIALS_END);
        while (MaterialPreloader.isWorking)
        {
            yield return null;
        }

        SceneManager.SetActiveScene(_targetScene);
        loadingProgress?.Invoke(PROGRESS_COMPLETE);

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

#if UNITY_EDITOR
    private bool IsSceneInBuildSettings(string sceneName)
    {
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled && System.IO.Path.GetFileNameWithoutExtension(scene.path) == sceneName)
            {
                return true;
            }
        }
        return false;
    }
#endif
}

// Helper class for Addressables scene loading compatibility
// Addressables scenes are already loaded and activated when LoadSceneAsync completes
internal class DummyAsyncOperation : AsyncOperation
{
    // This class is only used as a marker to indicate the scene was loaded from Addressables
    // The actual scene is already loaded and activated, so we don't need to do anything
}
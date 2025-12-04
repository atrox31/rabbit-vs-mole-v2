using EasyTransition;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GameSceneManager;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] TransitionSettings transitionSettings;
    private const string LOADING_SCENE_NAME = "Loading";

    private static SceneLoader _instance;
    private bool _isFirstLaunch = true;
    private bool _isTransitioning = false;
    private AsyncOperation _pendingLoadOperation;
    private string _pendingTargetSceneName;
    private bool _transitionCutPointReached = false;

    private TransitionManager GetTransitionManager()
    {
        var manager = TransitionManager.Instance();
        if (manager == null)
        {
            // Try to find TransitionManager in any loaded scene
            manager = FindFirstObjectByType<TransitionManager>();
            if (manager == null)
            {
                Debug.LogError("TransitionManager not found in any loaded scene. Make sure TransitionManager exists in at least one scene.");
            }
        }
        return manager;
    }


    private void Awake()
    {
        if(transitionSettings == null)
        {
            Debug.LogError("Transition Settings not assigned in SceneLoader.");
            DestroyImmediate(gameObject);
            return;
        }
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void ChangeScene(string targetSceneName, Action OnSceneStart)
    {
        if (_instance._isTransitioning)
        {
            Debug.LogWarning("Scene transition already in progress. Ignoring ChangeScene call.");
            return;
        }

        _instance.StartCoroutine(_instance.LoadSceneWithLoadingScreen(targetSceneName, OnSceneStart));
    }

    private IEnumerator LoadSceneWithLoadingScreen(string targetSceneName, Action OnSceneStart)
    {
        _isTransitioning = true;
        
        Scene currentScene = SceneManager.GetActiveScene();
        Scene loadingScene = SceneManager.GetSceneByName(LOADING_SCENE_NAME);
        TransitionManager transitionManager = GetTransitionManager();
        
        // STEP 1: Transition from current scene to loading screen
        // If this is the first launch and we're already in loadingScene, skip animation
        bool isFirstLaunchInLoadingScene = _isFirstLaunch && currentScene.name == LOADING_SCENE_NAME;
        
        if (currentScene.name != LOADING_SCENE_NAME)
        {
            // Normal transition from another scene to loading screen (with animation)
            if (transitionManager != null && transitionSettings != null)
            {
                // Start transition animation to loading screen
                _transitionCutPointReached = false;
                transitionManager.onTransitionCutPointReached += OnTransitionCutPointToLoading;
                transitionManager.Transition(transitionSettings, 0f);
                
                // Wait until animation covers the screen (cut point)
                yield return new WaitUntil(() => _transitionCutPointReached);
                
                // At cut point, screen is fully covered - switch scenes NOW
                // Load loading scene (if not already loaded)
                if (!loadingScene.IsValid())
                {
                    yield return SceneManager.LoadSceneAsync(LOADING_SCENE_NAME, LoadSceneMode.Additive);
                    loadingScene = SceneManager.GetSceneByName(LOADING_SCENE_NAME);
                }
                
                // Set loading scene as active before unloading the old scene
                if (loadingScene.IsValid())
                {
                    SceneManager.SetActiveScene(loadingScene);
                }
                
                // Now safely unload the old scene (we already have loading scene loaded)
                if (currentScene.IsValid() && currentScene != loadingScene && SceneManager.sceneCount > 1)
                {
                    yield return SceneManager.UnloadSceneAsync(currentScene);
                }
                
                transitionManager.onTransitionCutPointReached -= OnTransitionCutPointToLoading;
                
                // Wait for exit animation to complete (shows the new scene)
                yield return new WaitForSecondsRealtime(transitionSettings.destroyTime);
            }
            else
            {
                // No transition - just load loading scene directly
                if (!loadingScene.IsValid())
                {
                    yield return SceneManager.LoadSceneAsync(LOADING_SCENE_NAME, LoadSceneMode.Additive);
                    loadingScene = SceneManager.GetSceneByName(LOADING_SCENE_NAME);
                }
                
                if (loadingScene.IsValid())
                {
                    SceneManager.SetActiveScene(loadingScene);
                }
                
                if (currentScene.IsValid() && currentScene != loadingScene && SceneManager.sceneCount > 1)
                {
                    yield return SceneManager.UnloadSceneAsync(currentScene);
                }
            }
        }
        else if (isFirstLaunchInLoadingScene)
        {
            // First launch - we're already in loadingScene, don't do animation
            // Make sure loadingScene is active
            if (loadingScene.IsValid())
            {
                SceneManager.SetActiveScene(loadingScene);
            }
        }
        
        // STEP 2: Start loading target scene in background
        _pendingLoadOperation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
        _pendingLoadOperation.allowSceneActivation = false;
        _pendingTargetSceneName = targetSceneName;
        
        // Wait until target scene loads (up to 90%)
        while (_pendingLoadOperation.progress < 0.9f)
        {
            yield return null;
        }
        
        // Ensure target scene is actually loaded and valid before proceeding
        Scene targetSceneCheck = SceneManager.GetSceneByName(targetSceneName);
        if (!targetSceneCheck.IsValid())
        {
            Debug.LogError($"SceneLoader: Target scene '{targetSceneName}' is not valid after loading.");
            _isTransitioning = false;
            yield break;
        }
        
        // STEP 3: Transition from loading screen to target scene (with animation)
        // Always animate from loadingScene to target scene
        if (transitionManager != null && transitionSettings != null)
        {
            // Wait a moment to ensure previous animation has completed
            yield return new WaitForSecondsRealtime(0.3f);
            
            // Start transition animation to target scene
            _transitionCutPointReached = false;
            transitionManager.onTransitionCutPointReached += OnTransitionCutPointToTarget;
            transitionManager.Transition(transitionSettings, 0f);
            
            // Wait until animation covers the screen (cut point)
            yield return new WaitUntil(() => _transitionCutPointReached);
            
            // At cut point, screen is fully covered - switch scenes NOW
            yield return StartCoroutine(ActivateTargetScene());
            
            transitionManager.onTransitionCutPointReached -= OnTransitionCutPointToTarget;
            
            // Wait for exit animation to complete (shows the new scene)
            yield return new WaitForSecondsRealtime(transitionSettings.destroyTime);
        }
        else
        {
            // No transition - just activate target scene directly
            yield return StartCoroutine(ActivateTargetScene());
        }
        
        _isFirstLaunch = false;
        _isTransitioning = false;
        
        if(OnSceneStart != null)
        {
            OnSceneStart();
        }
    }
    
    private void OnTransitionCutPointToLoading()
    {
        _transitionCutPointReached = true;
    }
    
    private void OnTransitionCutPointToTarget()
    {
        _transitionCutPointReached = true;
    }
    
    private IEnumerator ActivateTargetScene()
    {
        if (_pendingLoadOperation != null && !_pendingLoadOperation.isDone)
        {
            _pendingLoadOperation.allowSceneActivation = true;
            yield return _pendingLoadOperation;
        }
        
        Scene targetScene = SceneManager.GetSceneByName(_pendingTargetSceneName);
        if (targetScene.IsValid())
        {
            SceneManager.SetActiveScene(targetScene);
            
            // Wait a frame to ensure target scene is fully initialized
            yield return null;
            yield return null;
            
            Scene loadingScene = SceneManager.GetSceneByName(LOADING_SCENE_NAME);
            if (loadingScene.IsValid() && loadingScene != targetScene && SceneManager.sceneCount > 1)
            {
                // Now that Bootstrap is the first scene (index 0), we can safely unload Loading
                yield return SceneManager.UnloadSceneAsync(loadingScene);
            }
        }
        
        _pendingLoadOperation = null;
        _pendingTargetSceneName = null;
    }
}
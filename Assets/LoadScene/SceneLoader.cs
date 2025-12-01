using EasyTransition;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public static void ChangeScene(string targetSceneName)
    {
        if (_instance._isTransitioning)
        {
            Debug.LogWarning("Scene transition already in progress. Ignoring ChangeScene call.");
            return;
        }

        _instance.StartCoroutine(_instance.LoadSceneWithLoadingScreen(targetSceneName));
    }

    private IEnumerator LoadSceneWithLoadingScreen(string targetSceneName)
    {
        _isTransitioning = true;
        
        if (_isFirstLaunch)
        {
            _isFirstLaunch = false;
            
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
            loadOperation.allowSceneActivation = false;
            
            while (loadOperation.progress < 0.9f)
            {
                yield return null;
            }
            
            _pendingLoadOperation = loadOperation;
            _pendingTargetSceneName = targetSceneName;
            _transitionCutPointReached = false;
            
            TransitionManager.Instance().onTransitionCutPointReached += OnTransitionCutPointToTarget;
            TransitionManager.Instance().Transition(transitionSettings, 0f);
            
            yield return new WaitUntil(() => _transitionCutPointReached);
            
            yield return StartCoroutine(ActivateTargetScene());
            
            TransitionManager.Instance().onTransitionCutPointReached -= OnTransitionCutPointToTarget;
        }
        else
        {
            
            Scene currentScene = SceneManager.GetActiveScene();
            Scene loadingScene = SceneManager.GetSceneByName(LOADING_SCENE_NAME);
            
            if (currentScene != loadingScene)
            {
                if (!loadingScene.IsValid())
                {
                    yield return SceneManager.LoadSceneAsync(LOADING_SCENE_NAME, LoadSceneMode.Additive);
                    loadingScene = SceneManager.GetSceneByName(LOADING_SCENE_NAME);
                }
                
                _transitionCutPointReached = false;
                TransitionManager.Instance().onTransitionCutPointReached += OnTransitionCutPointToLoading;
                TransitionManager.Instance().Transition(transitionSettings, 0f);
                
                yield return new WaitUntil(() => _transitionCutPointReached);
                
                if (loadingScene.IsValid())
                {
                    SceneManager.SetActiveScene(loadingScene);
                }
                
                TransitionManager.Instance().onTransitionCutPointReached -= OnTransitionCutPointToLoading;
                
                if (currentScene.IsValid() && currentScene != loadingScene)
                {
                    yield return SceneManager.UnloadSceneAsync(currentScene);
                }
            }
            
            _pendingLoadOperation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
            _pendingLoadOperation.allowSceneActivation = false;
            _pendingTargetSceneName = targetSceneName;
            
            while (_pendingLoadOperation.progress < 0.9f)
            {
                yield return null;
            }
            
            _transitionCutPointReached = false;
            TransitionManager.Instance().onTransitionCutPointReached += OnTransitionCutPointToTarget;
            TransitionManager.Instance().Transition(transitionSettings, 0f);
            
            yield return new WaitUntil(() => _transitionCutPointReached);
            
            yield return StartCoroutine(ActivateTargetScene());
            
            TransitionManager.Instance().onTransitionCutPointReached -= OnTransitionCutPointToTarget;
        }
        
        _isTransitioning = false;
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
            
            Scene loadingScene = SceneManager.GetSceneByName(LOADING_SCENE_NAME);
            if (loadingScene.IsValid())
            {
                yield return SceneManager.UnloadSceneAsync(loadingScene);
            }
        }
        
        _pendingLoadOperation = null;
        _pendingTargetSceneName = null;
    }
}
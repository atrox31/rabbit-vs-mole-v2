using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bootstrap scene script that automatically loads the Loading scene on game start.
/// This empty bootstrap scene (build index 0) allows Loading scene to be properly unloaded later.
/// </summary>
public class BootstrapScene : MonoBehaviour
{
    private const string LOADING_SCENE_NAME = "Loading";
    
    private void Start()
    {
        // Immediately load Loading scene as the first real scene
        // This allows Loading to be properly unloaded later since it's not build index 0
        SceneManager.LoadScene(LOADING_SCENE_NAME, LoadSceneMode.Single);
    }
}


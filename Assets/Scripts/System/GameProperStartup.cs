using UnityEngine;
using UnityEngine.SceneManagement;

public class GameProperStartup : MonoBehaviour
{
    private static bool hasStarted = false;
    [SerializeField] private bool backToActiveSceneAfterLoading;
    private void Awake()
    {
        if (hasStarted)
        {
            DestroyImmediate(gameObject);
            return;
        }
        hasStarted = true;


        if (backToActiveSceneAfterLoading)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            SceneLoader.ChangeScene(activeScene.name, null);
        }
        else
        {
            SceneManager.LoadScene("Loading", LoadSceneMode.Single);
        }
    }
}

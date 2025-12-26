using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameInitializer
{
    private static bool hasStarted = false;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnBeforeSceneLoad()
    {
        if (SceneManager.GetActiveScene().buildIndex != 0 && !hasStarted)
        {
            hasStarted = true;
            SceneManager.LoadScene(0);
        }
    }
}
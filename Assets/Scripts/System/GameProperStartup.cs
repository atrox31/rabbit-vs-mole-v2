using UnityEngine;
using UnityEngine.SceneManagement;

public class GameProperStartup : MonoBehaviour
{
    private static bool hasStarted = false;
    public static void BootstrapSceneActivation() { hasStarted = true; }
    void Awake()
    {
        if (hasStarted)
        {
            DestroyImmediate(gameObject);
            return;
        }
        hasStarted = true;
        Debug.ClearDeveloperConsole();
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }
}

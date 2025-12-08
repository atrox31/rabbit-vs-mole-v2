using UnityEngine;
using UnityEngine.SceneManagement;

public class GameProperStartup : MonoBehaviour
{
    private static bool hasStarted = false;
    void Awake()
    {
        if (hasStarted)
        {
            DestroyImmediate(gameObject);
            return;
        }
        hasStarted = true;
        SceneManager.LoadScene("Loading", LoadSceneMode.Single);
    }
}

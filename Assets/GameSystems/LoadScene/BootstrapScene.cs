using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bootstrap scene script that automatically loads the Loading scene on game start.
/// This empty bootstrap scene (build index 0) allows Loading scene to be properly unloaded later.
/// </summary>
public class BootstrapScene : MonoBehaviour
{
    [SerializeField] List<CoreBootstrapComponent> _coreGameObjects = new List<CoreBootstrapComponent>();


    IEnumerator Start()
    {
        // wait 1 frame to let everything initialize properly
        yield return null;

        foreach (var item in _coreGameObjects)
        {
            while (!item.IsReady)
            {
                yield return null;
            }
        }

        yield return null;
        foreach (var item in _coreGameObjects)
        {
            item.OnGameStart();
        }

    }
}


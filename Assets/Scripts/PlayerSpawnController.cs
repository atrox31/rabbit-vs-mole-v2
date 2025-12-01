using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PlayerSpawnController : MonoBehaviour
{
    private readonly string[] controlSchemes = {
        "Player 1",
        "Player 2"
    }; 

    private readonly Dictionary<PlayerType, string> prefabData = new Dictionary<PlayerType, string>{
       {PlayerType.Rabbit, "Assets/Prefabs/Rabbit.prefab" },
       {PlayerType.Mole, "Assets/Prefabs/Mole.prefab" }
    };

    private Dictionary<PlayerType, GameObject> _playerPrefab = new Dictionary<PlayerType, GameObject>();

    private PlayerInput _playerInput;
    private CinemachineBrain _cinemachineBrain;
    private CinemachineCamera _cinemachineCamera;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _cinemachineBrain = GetComponentInChildren<Camera>().GetComponent<CinemachineBrain>();
        _cinemachineCamera = GetComponentInChildren<CinemachineCamera>();

        _cinemachineBrain.enabled = false;

        foreach (var data in prefabData)
        {
            AsyncOperationHandle<GameObject> tempPrefab =
            Addressables.LoadAssetAsync<GameObject>(data.Value);

            var prefab = tempPrefab.WaitForCompletion();
            if(prefab == null)
            {
                Debug.LogError($"Can not load player({data.Key}) prefab '{data.Value}'");
            }
            else
            {
                _playerPrefab[data.Key] = prefab;
            }
        }
    }

    private IEnumerator Start()
    {
        yield return null;
        _cinemachineBrain.enabled = true;
    }

    public void Setup(PlayerType type)
    {
        var isRabbit = type == PlayerType.Rabbit;
        name = $"Player controller: {type.ToString()}";

        var instance = Instantiate(_playerPrefab[type], transform.position, quaternion.identity, transform);
        var target = instance.transform.Find("PlayerArmature");
        var controlScheme = isRabbit ? controlSchemes[0] : controlSchemes[1];

        _playerInput.SwitchCurrentControlScheme(controlScheme, Keyboard.current);
        _cinemachineCamera.Follow = target;

        _cinemachineCamera.Target = new CameraTarget
        {
            LookAtTarget = target,
            TrackingTarget = target
        };

        var channel = isRabbit
            ? OutputChannels.Channel01
            : OutputChannels.Channel02;

        _cinemachineBrain.ChannelMask = channel;
        _cinemachineCamera.OutputChannel = channel;
    }
}

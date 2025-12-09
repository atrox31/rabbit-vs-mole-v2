using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using Enums;
using Unity.VisualScripting;

/// <summary>
/// PlayerSpawnController - handles camera setup and player model instantiation.
/// </summary>
public class PlayerSpawnController : MonoBehaviour
{
    private static readonly Dictionary<PlayerType, string> PrefabPaths = new Dictionary<PlayerType, string>
    {
        { PlayerType.Rabbit, "Assets/Prefabs/Rabbit.prefab" },
        { PlayerType.Mole, "Assets/Prefabs/Mole.prefab" }
    };

    private Dictionary<PlayerType, GameObject> _playerPrefabs = new Dictionary<PlayerType, GameObject>();
    private PlayerInput _playerInput;
    private CinemachineBrain _cinemachineBrain;
    private CinemachineCamera _cinemachineCamera;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();

        var camera = GetComponentInChildren<Camera>();
        if (camera != null)
        {
            _cinemachineBrain = camera.GetComponent<CinemachineBrain>();
            _cinemachineCamera = GetComponentInChildren<CinemachineCamera>();
            
            if (_cinemachineBrain != null)
            {
                _cinemachineBrain.enabled = false;
            }
        }

        LoadPlayerPrefabs();
    }

    private void LoadPlayerPrefabs()
    {
        foreach (var kvp in PrefabPaths)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(kvp.Value);
            var prefab = handle.WaitForCompletion();
            
            if (prefab != null)
            {
                _playerPrefabs[kvp.Key] = prefab;
            }
        }
    }

    private void Start()
    {
        if (_cinemachineBrain != null)
        {
            _cinemachineBrain.enabled = true;
        }
    }

    public void Setup(PlayerType type, bool isSplitscreen, int playerIndex)
    {
        name = $"Player controller: {type}";

        if (!_playerPrefabs.TryGetValue(type, out var playerPrefab))
            return;

        var instance = Instantiate(playerPrefab, transform.position, Quaternion.identity, transform);
        var target = instance.transform.Find("PlayerArmature");
        
        if (target == null)
            return;

        SetupCamera(target, type, isSplitscreen, playerIndex);
        target.AddComponent<AudioLisnerController>();
    }

    private void SetupCamera(Transform target, PlayerType playerType, bool isSplitscreen, int playerIndex)
    {
        if (_cinemachineCamera == null || _cinemachineBrain == null)
            return;

        _cinemachineCamera.Follow = target;
        _cinemachineCamera.Target = new CameraTarget
        {
            LookAtTarget = target,
            TrackingTarget = target
        };

        var channel = playerType == PlayerType.Rabbit 
            ? OutputChannels.Channel01 
            : OutputChannels.Channel02;
        _cinemachineBrain.ChannelMask = channel;
        _cinemachineCamera.OutputChannel = channel;

        var camera = _cinemachineBrain.GetComponent<Camera>();
        if (camera != null)
        {
            camera.rect = isSplitscreen 
                ? new Rect(playerIndex * 0.5f, 0, 0.5f, 1)
                : new Rect(0, 0, 1, 1);
        }
    }
}

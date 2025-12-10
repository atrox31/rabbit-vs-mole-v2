using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using Enums;
using UnityEngine.SceneManagement;
using System.Dynamic;

/// <summary>
/// PlayerSpawnSystem - handles spawning players when a new map loads.
/// Supports story mode (1 player) and duel splitscreen mode (2 players).
/// </summary>
public class PlayerSpawnSystem : MonoBehaviour
{
    private static PlayerSpawnSystem _instance;

    private readonly Dictionary<PlayerType, string> _prefabPaths = new Dictionary<PlayerType, string>
    {
        { PlayerType.Rabbit, "Assets/Prefabs/Rabbit.prefab" },
        { PlayerType.Mole, "Assets/Prefabs/Mole.prefab" }
    };

    private Dictionary<PlayerType, GameObject> _playerPrefabs = new Dictionary<PlayerType, GameObject>();
    private GameObject _playerControllerPrefab;
    private PlayerInputManager _playerInputManager;
    private List<PlayerInput> _spawnedPlayers = new List<PlayerInput>();
    private Dictionary<PlayerType, Gamepad> _assignedGamepads = new Dictionary<PlayerType, Gamepad>();
    private List<Gamepad> _availableGamepads = new List<Gamepad>();

    private const string PlayerControllerPrefabPath = "Assets/Prefabs/Player.prefab";

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        _playerInputManager = GetComponent<PlayerInputManager>();
        if (_playerInputManager == null)
        {
            _playerInputManager = gameObject.AddComponent<PlayerInputManager>();
        }

        LoadPlayerPrefabs();
        LoadPlayerControllerPrefab();
    }

    private void LoadPlayerPrefabs()
    {
        foreach (var kvp in _prefabPaths)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(kvp.Value);
            var prefab = handle.WaitForCompletion();
            
            if (prefab != null)
            {
                _playerPrefabs[kvp.Key] = prefab;
            }
        }
    }

    private void LoadPlayerControllerPrefab()
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(PlayerControllerPrefabPath);
        _playerControllerPrefab = handle.WaitForCompletion();
        
        if (_playerControllerPrefab != null)
        {
            _playerInputManager.playerPrefab = _playerControllerPrefab;
        }
    }

    public static void CreateInstance(Scene scene)
    {
        Debug.Log("PlayerSpawnSystem: CreateInstance");
        if (_instance == null)
        {
            var go = new GameObject("PlayerSpawnSystem");
            _instance = go.AddComponent<PlayerSpawnSystem>();
            SceneManager.MoveGameObjectToScene(go, scene);
        }
        else
        {
            Debug.Log("PlayerSpawnSystem: instance exists");
        }
    }

    public static void SpawnPlayers()
    {
        Debug.Log("PlayerSpawnSystem: SpawnPlayers");
        if (!GameInspector.IsActive)
        {
            Debug.LogError("PlayerSpawnSystem: SpawnPlayers error! GameInspector is not active");
            return;
        }

        _instance.StartCoroutine(_instance.SpawnPlayersCoroutine());
    }

    private IEnumerator SpawnPlayersCoroutine()
    {
        ClearSpawnedPlayers();
        RefreshAvailableGamepads();

        var gameMode = GameInspector.CurrentGameMode;
        if (gameMode == null)
            yield break;

        var rabbitAgent = GameInspector.RabbitControlAgent;
        var moleAgent = GameInspector.MoleControlAgent;
        bool isSplitscreen = rabbitAgent == PlayerControlAgent.Human && moleAgent == PlayerControlAgent.Human;

        if (rabbitAgent == PlayerControlAgent.Human)
        {
            var spawnPoint = FindSpawnPoint(PlayerType.Rabbit);
            if (spawnPoint != null)
            {
                SpawnPlayer(PlayerType.Rabbit, spawnPoint.transform.position, isSplitscreen, 0);
            } else
            {
                DebugHelper.LogError(this, "Can not find spawnpoint for Rabbit");
            }
        }

        if (moleAgent == PlayerControlAgent.Human)
        {
            var spawnPoint = FindSpawnPoint(PlayerType.Mole);
            if (spawnPoint != null)
            {
                int playerIndex = isSplitscreen ? 1 : 0;
                SpawnPlayer(PlayerType.Mole, spawnPoint.transform.position, isSplitscreen, playerIndex);
            }
            else
            {
                DebugHelper.LogError(this, "Can not find spawnpoint for Rabbit");
            }
        }

        yield return null;
    }

    private void RefreshAvailableGamepads()
    {
        _availableGamepads.Clear();
        _assignedGamepads.Clear();

        foreach (var gamepad in Gamepad.all)
        {
            if (gamepad != null)
            {
                _availableGamepads.Add(gamepad);
            }
        }
    }

    private Gamepad GetGamepadForPlayer(PlayerType playerType)
    {
        if (_assignedGamepads.ContainsKey(playerType))
        {
            return _assignedGamepads[playerType];
        }

        int gamepadIndex = playerType == PlayerType.Rabbit ? 0 : 1;

        if (gamepadIndex < _availableGamepads.Count)
        {
            var gamepad = _availableGamepads[gamepadIndex];
            _assignedGamepads[playerType] = gamepad;
            return gamepad;
        }

        return null;
    }

    private PlayerSpawnPoint FindSpawnPoint(PlayerType playerType)
    {
        var spawnPoints = GameObject.FindGameObjectsWithTag("spawnPoint");
        
        foreach (var go in spawnPoints)
        {
            if (!go.activeInHierarchy)
                continue;

            var spawnPoint = go.GetComponent<PlayerSpawnPoint>();
            if (spawnPoint != null && spawnPoint.PlayerType == playerType)
            {
                return spawnPoint;
            }
        }

        return null;
    }

    private void SpawnPlayer(PlayerType playerType, Vector3 spawnPosition, bool isSplitscreen, int playerIndex)
    {
        if (_playerControllerPrefab == null)
            return;

        string controlScheme = GetControlScheme(playerType, isSplitscreen);
        var assignedGamepad = GetGamepadForPlayer(playerType);
        InputDevice device = assignedGamepad == null ? Keyboard.current : assignedGamepad;
        
        var playerInput = CreatePlayerInput(playerIndex, device, controlScheme);
        if (playerInput == null)
            return;

        SetupPlayerComponents(playerInput, playerType, spawnPosition, isSplitscreen, playerIndex);
        _spawnedPlayers.Add(playerInput);
    }

    private string GetControlScheme(PlayerType playerType, bool isSplitscreen)
    {
        if (GetGamepadForPlayer(playerType) != null)
        {
            return "Gamepad";
        }
        
        if (playerType == PlayerType.Mole && isSplitscreen)
        {
            return "KeyboardP2";
        }
        
        return "KeyboardP1";
    }

    private PlayerInput CreatePlayerInput(int playerIndex, InputDevice device, string controlScheme)
    {
        if (_playerControllerPrefab == null)
            return null;

        PlayerInput playerInput;

        if (device is Keyboard || device is Mouse)
        {
            playerInput = PlayerInput.Instantiate(_playerControllerPrefab, playerIndex: -1, pairWithDevice: device);
            
            if (!string.IsNullOrEmpty(controlScheme))
            {
                try
                {
                    playerInput.SwitchCurrentControlScheme(controlScheme, device);
                }
                catch
                {
                    // Use default control scheme
                }
            }
        }
        else if (device is Gamepad)
        {
            playerInput = PlayerInput.Instantiate(_playerControllerPrefab, playerIndex: -1, pairWithDevice: device, controlScheme: "Gamepad");
        }
        else
        {
            playerInput = PlayerInput.Instantiate(_playerControllerPrefab, playerIndex: -1, pairWithDevice: device);
        }
        
        if (playerInput == null)
            return null;

        playerInput.defaultActionMap = "Player";
        
        var playerActionMap = playerInput.actions?.FindActionMap("Player");
        playerActionMap?.Enable();
        
        return playerInput;
    }

    private void SetupPlayerComponents(PlayerInput playerInput, PlayerType playerType, Vector3 spawnPosition, 
        bool isSplitscreen, int playerIndex)
    {
        playerInput.transform.position = spawnPosition;

        var playerSpawnController = playerInput.GetComponent<PlayerSpawnController>();
        if (playerSpawnController != null)
        {
            playerSpawnController.Setup(playerType, isSplitscreen, playerIndex);
        }
    }

    private void ClearSpawnedPlayers()
    {
        foreach (var player in _spawnedPlayers)
        {
            if (player != null)
            {
                Destroy(player.gameObject);
            }
        }
        _spawnedPlayers.Clear();
        _assignedGamepads.Clear();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}

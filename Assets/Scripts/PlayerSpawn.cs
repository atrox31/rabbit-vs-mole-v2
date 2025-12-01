using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawn : MonoBehaviour
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private Transform _playerSpawnRabbit;
    [SerializeField] private Transform _playerSpawnMole;
    [SerializeField] private bool _shouldSpawnRabbit = true;
    [SerializeField] private bool _shouldSpawnMole = true;

    private PlayerInputManager _playerInputManager;

    private readonly IReadOnlyDictionary<PlayerType, string> _controlSchemes = new Dictionary<PlayerType, string>
    {
        { PlayerType.Rabbit, "Player 1" },
        { PlayerType.Mole, "Player 2" }
    };

    private void Awake()
    {
        Debug.Log(_playerSpawnRabbit.position);
        _playerInputManager = GetComponent<PlayerInputManager>();
        _playerInputManager.playerPrefab = _playerPrefab;
        if (_shouldSpawnRabbit) SpawnPlayer(PlayerType.Rabbit, _playerSpawnRabbit.position);
        if (_shouldSpawnMole) SpawnPlayer(PlayerType.Mole, _playerSpawnMole.position);
    }

    private void Start()
    {
    }

    private void SpawnPlayer(PlayerType type, Vector3 spawnPosition)
    {
        var controlScheme = _controlSchemes.GetValueOrDefault(type);
        var player = _playerInputManager.JoinPlayer(pairWithDevice: Keyboard.current, controlScheme: controlScheme);
        player.GetComponentInParent<PlayerSpawnController>().Setup(type);
        StartCoroutine(SetPositionDelayed(player.transform, spawnPosition));
    }

    private IEnumerator SetPositionDelayed(Transform player, Vector3 target)
    {
        yield return null; // Czekaj jedn¹ klatkê
        player.SetPositionAndRotation(target, Quaternion.identity);
    }
}
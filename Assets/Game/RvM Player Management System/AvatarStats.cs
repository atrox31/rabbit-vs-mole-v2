using System.Runtime.CompilerServices;
using UnityEngine;

public class AvatarStats
{
    [SerializeField] private float _baseMaxWalkSpeed = 5f;
    [SerializeField] private float _baseAcceleration = 10f;
    [SerializeField] private float _baseDeceleration = 20f;

    private float _maxWalkSpeedMultiplier = 1f;

    public float MaxWalkSpeed => _baseMaxWalkSpeed * _maxWalkSpeedMultiplier;
    public float Acceleration => _baseAcceleration;
    public float Deceleration => _baseDeceleration;
    public float RotationSpeed => _baseMaxWalkSpeed;

    public AvatarStats() { }
    public AvatarStats(float baseMaxWalkSpeed)
    {
        _baseMaxWalkSpeed = baseMaxWalkSpeed;
    }
    public void SetMaxWalkSpeedMultiplier(float multiplier) => _maxWalkSpeedMultiplier = multiplier;
}
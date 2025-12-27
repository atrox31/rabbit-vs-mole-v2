using RabbitVsMole;
using System.Xml.Serialization;
using UnityEngine;

public class FarmFieldTileHighlighter : MonoBehaviour
{
    private static FarmFieldTileHighlighter _rabbitSingleton;
    private static FarmFieldTileHighlighter _moleSingleton;

    public static FarmFieldTileHighlighter Instance(PlayerType playerType) =>
        playerType switch
        {
            PlayerType.Rabbit => _rabbitSingleton,
            PlayerType.Mole => _moleSingleton,
            _ => null // Safer than throwing exception in UI/Visual code
        };

    public void Setup(PlayerType playerType)
    {
        // 1. Assign singleton based on player type
        if (playerType == PlayerType.Rabbit) _rabbitSingleton = this;
        else if (playerType == PlayerType.Mole) _moleSingleton = this;

        // 2. Setup Layer
        gameObject.layer = LayerMask.NameToLayer(playerType.ToString());

        // 3. Ensure it starts hidden
        HideForSure();
    }

    // Clear static references when the object is destroyed (e.g., scene change)
    private void OnDestroy()
    {
        if (_rabbitSingleton == this) _rabbitSingleton = null;
        if (_moleSingleton == this) _moleSingleton = null;
    }

    [Header("Movement")]
    [SerializeField] private float _smoothSpeed = 15f;
    [SerializeField] private float _yOffset = 0.02f;

    [Header("Visual Juice")]
    [SerializeField] private float _hoverHeight = 0.05f;
    [SerializeField] private float _bounceSpeed = 3f;

    [SerializeField] private float _activeTimerTreshold = .1f;

    private Vector3 _targetPos;
    private bool _active;
    private float _activeTimer = 0f;
    private bool _isPendingHide = false;

    public void SetTarget(Vector3 tilePosition)
    {
        if (!_active)
        {
            _active = true;
            gameObject.SetActive(true);
            // Snap to initial position to avoid flying across the map on first show
            transform.position = tilePosition + Vector3.up * _yOffset;
        }
        _targetPos = tilePosition;

        _activeTimer = 0f;
        _isPendingHide = false;
    }

    public void Hide()
    {
        _isPendingHide = true;
    }

    private void HideForSure()
    {
        _active = false;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!_active) return;

        if (_isPendingHide)
        {
            _activeTimer += Time.deltaTime;
            if (_activeTimer > _activeTimerTreshold)
            {
                HideForSure();
                return;
            }
        }

        // Hover animation logic
        float hover = Mathf.Sin(Time.time * _bounceSpeed) * _hoverHeight;
        Vector3 finalTarget = _targetPos + Vector3.up * (_yOffset + hover);

        transform.position = Vector3.Lerp(transform.position, finalTarget, Time.deltaTime * _smoothSpeed);
    }
}
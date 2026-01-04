using GameSystems;
using Interface.Element;
using PlayerManagementSystem.Backpack;
using PlayerManagementSystem.Backpack.Events;
using RabbitVsMole;
using RabbitVsMole.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("Game timer")]
    [SerializeField] GameObject _gameTImerPanel;
    [SerializeField] TextMeshProUGUI _gameTimerTMP;

    [Header("Carrot counter")]
    [SerializeField] GameObject _rabbitCarrotCounterPanel;
    [SerializeField] TextMeshProUGUI _rabbitCarrotCounterTMP;
    [SerializeField] GameObject _moleCarrotCounterPanel;
    [SerializeField] TextMeshProUGUI _moleCarrotCounterTMP;

    [Header("Inventory Rabbit")]
    [SerializeField] GameObject _inwentoryRabbitPanel;
    [SerializeField] TextMeshProUGUI _inwentoryRabbitSeedCounter;
    [SerializeField] TextMeshProUGUI _inwentoryRabbitWaterCounter;
    [SerializeField] UnityEngine.UI.Image _inwentoryRabbitSeedImage;
    [SerializeField] UnityEngine.UI.Image _inwentoryRabbitWaterImage;

    [Header("Inventory Mole")]
    [SerializeField] GameObject _inwentoryMolePanel;
    [SerializeField] TextMeshProUGUI _inwentoryMoleDirtCounter;
    [SerializeField] UnityEngine.UI.Image _inwentoryMoleDirtImage;
    [SerializeField] TextMeshProUGUI _inwentoryMoleHealthCounter;
    [SerializeField] UnityEngine.UI.Image _inwentoryMoleHealthImage;

    [Header("Game over")]
    [SerializeField] GameObject _gameOverPanel;
    [SerializeField] TextMeshProUGUI _gameOverTitle;
    [SerializeField] TextMeshProUGUI _gameOverText;
    [SerializeField] GameObject _gameOverDefaultButton;

    [Header("Audio")]
    [SerializeField] AudioClip _LastSecondsSound;

    private void Awake()
    {
        // Hide inventory panels immediately on creation
        // They will be shown via SetInventoryVisible() when needed
        if (_inwentoryRabbitPanel != null) _inwentoryRabbitPanel.SetActive(false);
        if (_inwentoryMolePanel != null) _inwentoryMolePanel.SetActive(false);
        if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<InventoryChangedEvent>(UpdateItem);
        EventBus.Subscribe<InventoryErrorEvent>(ErrorItem);
        EventBus.Subscribe<TimeUpdateEvent>(UpdateTimer);
        EventBus.Subscribe<CarrotPickEvent>(UpdateCarrots);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<InventoryChangedEvent>(UpdateItem);
        EventBus.Unsubscribe<InventoryErrorEvent>(ErrorItem);
        EventBus.Unsubscribe<TimeUpdateEvent>(UpdateTimer);
        EventBus.Unsubscribe<CarrotPickEvent>(UpdateCarrots);
    }

    public void ButtonRestartGame() =>
        GameManager.RestartGame();

    public void ButtonReturnToMenu() =>
        GameManager.GoToMainMenu();

    public void ShowGameOverScreen(string text, string title)
    {
        if (_gameOverPanel.activeSelf)
            return;

        _gameOverTitle.text = title;
        _gameOverText.text = text;

        OnDisable();
        StopAllCoroutines();
        StartCoroutine(AnimateGameOverScreen());
    }

    IEnumerator AnimateGameOverScreen()
    {
        float elapsedTime = 0f;
        const float duration = .13f;
        _gameOverPanel.SetActive(true);

        var panels = new List<GameObject>()
        { _gameOverPanel };

        SetPanelsScale(0f, panels);
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);
            SetPanelsScale(progress, panels);
            yield return null;
        }
        SetPanelsScale(1f, panels);


        EventSystem _eventSystem = GetComponentInChildren<EventSystem>();
        if (_eventSystem == null)
            DebugHelper.LogError(this, "Event system is not found");

        _eventSystem.SetSelectedGameObject(_gameOverDefaultButton);
    }

    void UpdateItem(InventoryChangedEvent inventoryChangedEvent)
    {
        switch (inventoryChangedEvent.BackpackItemType)
        {
            case BackpackItemType.Seed:
                ShowItemGet(_inwentoryRabbitSeedCounter, _inwentoryRabbitSeedImage, inventoryChangedEvent);
                break;
            case BackpackItemType.Water:
                ShowItemGet(_inwentoryRabbitWaterCounter, _inwentoryRabbitWaterImage, inventoryChangedEvent);
                break;
            case BackpackItemType.Dirt:
                ShowItemGet(_inwentoryMoleDirtCounter, _inwentoryMoleDirtImage, inventoryChangedEvent);
                break;
            case BackpackItemType.Health:
                ShowItemGet(_inwentoryMoleHealthCounter, _inwentoryMoleHealthImage, inventoryChangedEvent);
                break;
            case BackpackItemType.Carrot:
                break;
            default:
                break;
        }
    }

    void ErrorItem(InventoryErrorEvent inventoryErrorEvent)
    {
        switch (inventoryErrorEvent.BackpackItemType)
        {
            case BackpackItemType.Seed:
                ShowItemError(_inwentoryRabbitSeedCounter, _inwentoryRabbitSeedImage);
                break;
            case BackpackItemType.Water:
                ShowItemError(_inwentoryRabbitWaterCounter, _inwentoryRabbitWaterImage);
                break;
            case BackpackItemType.Dirt:
                ShowItemError(_inwentoryMoleDirtCounter, _inwentoryMoleDirtImage);
                break;
            case BackpackItemType.Health:
                ShowItemError(_inwentoryMoleHealthCounter, _inwentoryMoleHealthImage);
                break;
            case BackpackItemType.Carrot:
                break;
            default:
                break;
        }
    }

    private float _nextAllowedSoundTime = 0f;
    void UpdateTimer(RabbitVsMole.Events.TimeUpdateEvent eventData)
    {
        _gameTimerTMP.text = $"{eventData.Minutes:00}:{eventData.Seconds:00}";

        if (!eventData.IsEndingTime)
            return;

        PopTextColor(_gameTimerTMP, Color.red, 0.99f);

        if (_LastSecondsSound == null)
            return;

        if (_LastSecondsSound != null && Time.time >= _nextAllowedSoundTime)
        {
            _nextAllowedSoundTime = Time.time + 1f;
            AudioManager.PlaySoundUI(_LastSecondsSound);
        }
    }

    void UpdateCarrots(RabbitVsMole.Events.CarrotPickEvent eventData)
    {
        switch (eventData.PlayerType)
        {
            case PlayerType.Rabbit:
                _rabbitCarrotCounterTMP.text = eventData.Count.ToString();
                PopText(_rabbitCarrotCounterTMP, 0.6f, .3f);
                break;
            case PlayerType.Mole:
                _moleCarrotCounterTMP.text = eventData.Count.ToString();
                PopText(_moleCarrotCounterTMP, 0.6f, .3f);
                break;
            default:
                break;
        }
    }

    void ShowItemGet(TextMeshProUGUI text, UnityEngine.UI.Image image, InventoryChangedEvent inventoryChangedEvent)
    {
        if(inventoryChangedEvent.Capacity == 100)
            text.text = $"{Mathf.RoundToInt( (float)inventoryChangedEvent.Count/ (float)inventoryChangedEvent.Capacity*100f)}%";
        else
            text.text = $"{inventoryChangedEvent.Count}/{inventoryChangedEvent.Capacity}";

        PopText(text, .7f, .2f);
        PopImage(image, .3f, .1f);
    }

    void ShowItemError(TextMeshProUGUI text, UnityEngine.UI.Image image)
    {
        PopTextColor(text, Color.softRed, .4f);
        PopImage(image, .2f, .1f);
    }

    IEnumerator AnimatePop(float duration, Action<float> onUpdate, Action onComplete)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Min(elapsedTime / duration, 1f);

            float sinValue = Mathf.Sin(progress * Mathf.PI);

            onUpdate(sinValue);
            yield return null;
        }

        onComplete?.Invoke();
    }

    // Per-image tracking for animations
    Dictionary<Image, Coroutine> _imageCoroutines = new Dictionary<Image, Coroutine>();
    Dictionary<Image, Vector3> _imageOriginalScales = new Dictionary<Image, Vector3>();

    /// <summary>
    /// Sets the pivot of a RectTransform without changing its visual position
    /// </summary>
    void SetPivotWithoutMoving(RectTransform rectTransform, Vector2 newPivot)
    {
        Vector2 size = rectTransform.rect.size;
        Vector2 deltaPivot = newPivot - rectTransform.pivot;
        Vector3 deltaPosition = new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y, 0f);
        rectTransform.pivot = newPivot;
        rectTransform.localPosition += deltaPosition;
    }

    public void PopImage(Image image, float duration, float amount = .1f)
    {
        if (image == null) return;

        // Store original scale only if we don't have it yet (first animation)
        if (!_imageOriginalScales.ContainsKey(image))
        {
            _imageOriginalScales[image] = image.rectTransform.localScale;
            // Ensure pivot is centered so scaling happens evenly in all directions
            SetPivotWithoutMoving(image.rectTransform, new Vector2(0.5f, 0.5f));
        }

        // Stop previous animation for this specific image
        if (_imageCoroutines.TryGetValue(image, out Coroutine existingCoroutine) && existingCoroutine != null)
        {
            StopCoroutine(existingCoroutine);
        }

        Vector3 originalScale = _imageOriginalScales[image];

        var newCoroutine = StartCoroutine(AnimatePop(duration,
            (sin) =>
            {
                if (image != null)
                    image.rectTransform.localScale = originalScale * (1f + sin * amount);
            },
            () =>
            {
                if (image != null)
                    image.rectTransform.localScale = originalScale;
                _imageCoroutines.Remove(image);
            }
        ));

        _imageCoroutines[image] = newCoroutine;
    }

    // Per-text tracking for scale animations
    Dictionary<TextMeshProUGUI, Coroutine> _textScaleCoroutines = new Dictionary<TextMeshProUGUI, Coroutine>();
    Dictionary<TextMeshProUGUI, Vector3> _textOriginalScales = new Dictionary<TextMeshProUGUI, Vector3>();

    public void PopText(TextMeshProUGUI text, float duration, float amount = .1f)
    {
        if (text == null) return;

        // Store original scale only if we don't have it yet (first animation)
        if (!_textOriginalScales.ContainsKey(text))
        {
            _textOriginalScales[text] = text.transform.localScale;
        }

        // Stop previous animation for this specific text
        if (_textScaleCoroutines.TryGetValue(text, out Coroutine existingCoroutine) && existingCoroutine != null)
        {
            StopCoroutine(existingCoroutine);
        }

        Vector3 originalScale = _textOriginalScales[text];

        var newCoroutine = StartCoroutine(AnimatePop(duration,
            (sin) =>
            {
                if (text != null)
                    text.transform.localScale = originalScale * (1f + sin * amount);
            },
            () =>
            {
                if (text != null)
                    text.transform.localScale = originalScale;
                _textScaleCoroutines.Remove(text);
            }
        ));

        _textScaleCoroutines[text] = newCoroutine;
    }

    // Per-text tracking for color animations
    Dictionary<TextMeshProUGUI, Coroutine> _textColorCoroutines = new Dictionary<TextMeshProUGUI, Coroutine>();
    Dictionary<TextMeshProUGUI, Color> _textOriginalColors = new Dictionary<TextMeshProUGUI, Color>();

    public void PopTextColor(TextMeshProUGUI text, Color targetColor, float duration)
    {
        if (text == null) return;

        // Store original color only if we don't have it yet (first animation)
        if (!_textOriginalColors.ContainsKey(text))
        {
            _textOriginalColors[text] = text.color;
        }

        // Stop previous animation for this specific text
        if (_textColorCoroutines.TryGetValue(text, out Coroutine existingCoroutine) && existingCoroutine != null)
        {
            StopCoroutine(existingCoroutine);
        }

        Color originalColor = _textOriginalColors[text];

        var newCoroutine = StartCoroutine(AnimatePop(duration,
            (sin) =>
            {
                if (text != null)
                    text.color = Color.Lerp(originalColor, targetColor, sin);
            },
            () =>
            {
                if (text != null)
                    text.color = originalColor;
                _textColorCoroutines.Remove(text);
            }
        ));

        _textColorCoroutines[text] = newCoroutine;
    }

    private void SetPanelsScale(float scale, List<GameObject> panelsToAnimate)
    {
        foreach (var panel in panelsToAnimate)
        {
            if (panel != null) panel.transform.localScale = new Vector3(scale, scale, scale);
        }
    }

    Dictionary<GameObject, Coroutine> _panelVisibilityCoroutines = new Dictionary<GameObject, Coroutine>();

    public void SetInventoryVisible(PlayerType playerType, bool visible)
    {
        GameObject panel = null;
        switch (playerType)
        {
            case PlayerType.Rabbit:
                panel = _inwentoryRabbitPanel;
                break;
            case PlayerType.Mole:
                panel = _inwentoryMolePanel;
                break;
            default:
                return;
        }

        if (panel == null) return;

        // Stop any existing animation for this panel
        if (_panelVisibilityCoroutines.TryGetValue(panel, out Coroutine existingCoroutine) && existingCoroutine != null)
        {
            StopCoroutine(existingCoroutine);
        }

        if (visible)
        {
            // Set scale to 0 before activating so animation starts from invisible
            panel.transform.localScale = Vector3.zero;
            panel.SetActive(true);
            _panelVisibilityCoroutines[panel] = StartCoroutine(AnimatePanelVisibility(panel, true));
        }
        else
        {
            // Only animate if panel is currently active
            if (panel.activeSelf)
            {
                _panelVisibilityCoroutines[panel] = StartCoroutine(AnimatePanelVisibility(panel, false));
            }
        }
    }

    IEnumerator AnimatePanelVisibility(GameObject panel, bool show)
    {
        const float duration = 0.2f;
        float elapsedTime = 0f;
        float startScale = show ? 0f : 1f;
        float endScale = show ? 1f : 0f;

        panel.transform.localScale = new Vector3(startScale, startScale, startScale);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);
            // Use ease-out for show, ease-in for hide
            float easedProgress = show 
                ? 1f - Mathf.Pow(1f - progress, 2f) // ease-out
                : Mathf.Pow(progress, 2f); // ease-in
            float scale = Mathf.Lerp(startScale, endScale, easedProgress);
            panel.transform.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }

        panel.transform.localScale = new Vector3(endScale, endScale, endScale);

        if (!show)
        {
            panel.SetActive(false);
        }

        _panelVisibilityCoroutines.Remove(panel);
    }

    IEnumerator Start()
    {
        float elapsedTime = 0f; 
        const float duration = .33f;

        var panelsToAnimate = new List<GameObject>() {
            _gameTImerPanel,
            _rabbitCarrotCounterPanel,
            _moleCarrotCounterPanel
        };

        _gameTimerTMP.text = "00:00"; 
        _rabbitCarrotCounterTMP.text = "0";
        _moleCarrotCounterTMP.text = "0";
        _inwentoryRabbitSeedCounter.text = $"0/{GameManager.CurrentGameStats.BackpackCapacitySeed}";
        _inwentoryRabbitWaterCounter.text = $"0/{GameManager.CurrentGameStats.BackpackCapacityWater}";
        _inwentoryMoleDirtCounter.text = $"0/{GameManager.CurrentGameStats.BackpackCapacityDirt}";
        _inwentoryMoleHealthCounter.text = "100%";


        SetPanelsScale(0f, panelsToAnimate);
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);
            SetPanelsScale(progress, panelsToAnimate);
            yield return null;
        }
        SetPanelsScale(1f, panelsToAnimate);
    }

}

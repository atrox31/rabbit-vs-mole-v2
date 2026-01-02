using GameSystems;
using PlayerManagementSystem.Backpack;
using PlayerManagementSystem.Backpack.Events;
using RabbitVsMole;
using RabbitVsMole.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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
    [SerializeField] TextMeshProUGUI _gameOverText;

    [Header("Audio")]
    [SerializeField] AudioClip _LastSecondsSound;

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

    public void ShowGameOverScreen(string text)
    {
        if (_gameOverPanel.activeSelf)
            return;

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

    private void RunAnimation<T>(
        Coroutine activeCoroutine,
        Action restoreAction,
        Action<Coroutine> setActiveCoroutine,
        Action<Action> setRestoreAction,
        T originalValue,
        Action<T> applyValue,
        float duration,
        Func<float, T, T> calculateValue
    )
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            restoreAction?.Invoke();
        }

        Action newRestoreAction = () => applyValue(originalValue);
        setRestoreAction(newRestoreAction);

        var newCoroutine = StartCoroutine(AnimatePop(duration,
            (sin) =>
            {
                T val = calculateValue(sin, originalValue);
                applyValue(val);
            },
            () =>
            {
                applyValue(originalValue);
                setActiveCoroutine(null);
                setRestoreAction(null);
            }
        ));

        setActiveCoroutine(newCoroutine);
    }

    Coroutine _popImageCoroutine;
    Action _popImageRestore;
    public void PopImage(UnityEngine.UI.Image image, float duration, float amount = .1f)
    {
        RunAnimation(
            _popImageCoroutine,
            _popImageRestore,
            (c) => _popImageCoroutine = c,
            (a) => _popImageRestore = a,
            image.rectTransform.localScale,
            (val) => { if (image != null) image.rectTransform.localScale = val; },
            duration,
            (sin, original) => original + (original * (sin * amount))
        );
    }

    Coroutine _popTextCoroutine;
    Action _popTextRestore;
    public void PopText(TextMeshProUGUI text, float duration, float amount = .1f)
    {
        RunAnimation(
            _popTextCoroutine,
            _popTextRestore,
            (c) => _popTextCoroutine = c,
            (a) => _popTextRestore = a,
            text.transform.localScale,
            (val) => { if (text != null) text.transform.localScale = val; },
            duration,
            (sin, original) => original + (original * (sin * amount)) // Logic: original * (1 + sin*amount) = original + original*sin*amount
        );
    }

    Coroutine _popTextColorCoroutine;
    Action _popTextColorRestore;
    public void PopTextColor(TextMeshProUGUI text, Color targetColor, float duration)
    {
        RunAnimation(
            _popTextColorCoroutine,
            _popTextColorRestore,
            (c) => _popTextColorCoroutine = c,
            (a) => _popTextColorRestore = a,
            text.color,
            (val) => { if (text != null) text.color = val; },
            duration,
            (sin, original) => Color.Lerp(original, targetColor, sin)
        );
    }

    private void SetPanelsScale(float scale, List<GameObject> panelsToAnimate)
    {
        foreach (var panel in panelsToAnimate)
        {
            if (panel != null) panel.transform.localScale = new Vector3(scale, scale, scale);
        }
    }

    public void SetInventoryVisible(PlayerType playerType, bool visible)
    {
        switch (playerType)
        {
            case PlayerType.Rabbit:
                if (_inwentoryRabbitPanel == null) return;
                _inwentoryRabbitPanel.SetActive(visible);
                break;
            case PlayerType.Mole:
                if (_inwentoryMolePanel == null) return;
                _inwentoryMolePanel.SetActive(visible);
                break;
            default:
                break;
        }
    }

    IEnumerator Start()
    {
        float elapsedTime = 0f; 
        const float duration = .33f;

        var panelsToAnimate = new List<GameObject>() {
            _gameTImerPanel,
            _rabbitCarrotCounterPanel,
            _moleCarrotCounterPanel,
            _inwentoryRabbitPanel,
            _inwentoryMolePanel
        };

        _gameOverPanel.SetActive(false);

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

using GameSystems;
using PlayerManagementSystem.Backpack;
using RabbitVsMole;
using RabbitVsMole.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

public class GameUI : MonoBehaviour
{
    [Header("Game timer")]
    [SerializeField] Panel _gameTImerPanel;
    [SerializeField] TextMeshProUGUI _gameTimerTMP;

    [Header("Carrot counter")]
    [SerializeField] Panel _rabbitCarrotCounterPanel;
    [SerializeField] TextMeshProUGUI _rabbitCarrotCounterTMP;
    [SerializeField] Panel _moleCarrotCounterPanel;
    [SerializeField] TextMeshProUGUI _moleCarrotCounterTMP;

    [Header("Game timer")]
    [SerializeField] Panel _inwentoryRabbitPanel;
    [SerializeField] TextMeshProUGUI _inwentoryRabbitSeedCounter;
    [SerializeField] TextMeshProUGUI _inwentoryRabbitWaterCounter;
    [SerializeField] UnityEngine.UI.Image _inwentoryRabbitSeedImage;
    [SerializeField] UnityEngine.UI.Image _inwentoryRabbitWaterImage;

    [SerializeField] Panel _inwentoryMolePanel;

    [Header("Audio")]
    [SerializeField] AudioClip _LastSecondsSound;

    private void OnEnable()
    {
        EventBus.Subscribe<InventoryChangedEvent>(UpdateItem);
        EventBus.Subscribe<TimeUpdateEvent>(UpdateTimer);
        EventBus.Subscribe<CarrotPickEvent>(UpdateCarrots);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<InventoryChangedEvent>(UpdateItem);
        EventBus.Unsubscribe<TimeUpdateEvent>(UpdateTimer);
        EventBus.Unsubscribe<CarrotPickEvent>(UpdateCarrots);
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
            _nextAllowedSoundTime = Time.time + _LastSecondsSound.length;
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
        text.text = $"{inventoryChangedEvent.Count}/{inventoryChangedEvent.Capacity}";
        PopText(text, .9f, .3f);
        PopImage(image, 1.1f, .2f);
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

    public void PopImage(UnityEngine.UI.Image image, float duration, float amount = .1f)
    {
        var oryginalScale = image.rectTransform.localScale;
        StartCoroutine( AnimatePop(duration,
            (sin) =>
            {
                float val = sin * amount;
                image.rectTransform.localScale = oryginalScale + (oryginalScale * val);
            },
            () => image.rectTransform.localScale = oryginalScale
        ));
    }

    public void PopText(TextMeshProUGUI text, float duration, float amount = .1f)
    {
        Vector3 originalScale = text.transform.localScale;
        StartCoroutine(AnimatePop(duration,
            (sinValue) =>
            {
                float scaleMultiplier = 1f + (sinValue * amount);
                text.transform.localScale = originalScale * scaleMultiplier;
            },
            () =>
            {
                text.transform.localScale = originalScale;
            }
        ));
    }

    public void PopTextColor(TextMeshProUGUI text, Color targetColor, float duration)
    {
        Color originalColor = text.color;
        StartCoroutine(AnimatePop(duration,
            (sin) => text.color = Color.Lerp(originalColor, targetColor, sin),
            () => text.color = originalColor
        ));
    }
    private void SetPanelsScale(float scale, List<Panel> panelsToAnimate)
    {
        foreach (var panel in panelsToAnimate)
        {
            if (panel != null) panel.style.scale = new StyleScale(new Vector2(scale, scale));
        }
    }

    public void SetInventoryVisible(PlayerType playerType, bool visible)
    {
        switch (playerType)
        {
            case PlayerType.Rabbit:
                if (_inwentoryRabbitPanel == null) return;
                _inwentoryRabbitPanel.visible = visible;
                break;
            case PlayerType.Mole:
                if (_inwentoryMolePanel == null) return;
                _inwentoryMolePanel.visible = visible;
                break;
            default:
                break;
        }
    }

    IEnumerator Start()
    {
        float elapsedTime = 0f; 
        const float duration = 5f;

        var panelsToAnimate = new List<Panel>() {
            _gameTImerPanel,
            _rabbitCarrotCounterPanel,
            _moleCarrotCounterPanel,
            _inwentoryRabbitPanel,
            _inwentoryMolePanel
        };

        _gameTimerTMP.text = "00:00"; 
        _rabbitCarrotCounterTMP.text = "0";
        _moleCarrotCounterTMP.text = "0";
        _inwentoryRabbitSeedCounter.text = "0/0";
        _inwentoryRabbitWaterCounter.text = "0/0";


        SetPanelsScale(0f, panelsToAnimate);
        //TODO: to wo ogóle nie dzia³a -.-
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

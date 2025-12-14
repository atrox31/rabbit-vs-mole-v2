using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;

[Obsolete]
public class GUICustomElement_DaySelector : Interface.Element.InterfaceElement
{
    [Header("Day Selector References")]
    [SerializeField] private TextMeshProUGUI _dayText;
    [SerializeField] private Button _previousDayButton;
    [SerializeField] private Button _nextDayButton;
    [SerializeField] private Image _levelStatusImage;
    [SerializeField] private Image _backgroundImage;

    [Header("Day Background Images")]
    [SerializeField] private Sprite[] _dayBackgrounds = new Sprite[7]; // Monday to Sunday

    [Header("Day Names Localization")]
    [SerializeField] private LocalizedString[] _dayNames = new LocalizedString[7]; // Monday to Sunday

    [Header("Level Status Sprites")]
    [SerializeField] private Sprite _goldenCarrot;
    [SerializeField] private Sprite _levelComplite;

    [Header("Animation Settings")]
    [SerializeField] private float _slideAnimationDuration = 0.3f;

    private DayOfWeek _currentDay = DayOfWeek.Monday;
    
    [SerializeField] private bool _allowAnchorModificationOverride = false;
    private bool _isAnimating2 = false;
    private PlayerType _currentPlayerType;
    public override bool AllowAnchorModification => _allowAnchorModificationOverride;

    public override void InitializeWithArgument(object argument)
    {
        _currentPlayerType = (PlayerType)argument;
    }

    private int GetDayIndex(DayOfWeek day)
    {
        // Monday(1) -> 0, Tuesday(2) -> 1, ..., Sunday(0) -> 6
        if (day == DayOfWeek.Sunday)
            return 6;
        return (int)day - 1;
    }

    private DayOfWeek GetDayFromIndex(int index)
    {
        // 0 -> Monday(1), 1 -> Tuesday(2), ..., 6 -> Sunday(0)
        if (index == 6)
            return DayOfWeek.Sunday;
        return (DayOfWeek)(index + 1);
    }

    private bool IsFirstDay()
    {
        return _currentDay == DayOfWeek.Monday;
    }

    private bool IsLastDay()
    {
        return _currentDay == DayOfWeek.Sunday;
    }

    private LocalizedString GetDayName(DayOfWeek day)
    {
        int dayIndex = GetDayIndex(day);
        if (dayIndex >= 0 && dayIndex < _dayNames.Length && _dayNames[dayIndex].IsEmpty == false)
        {
            return _dayNames[dayIndex];
        }
        
        return new LocalizedString();
    }


    protected override void Setup()
    {
        base.Setup();
        InitializeDay();
        AttachButtonHandlers();
        UpdateUI();
        FixCustomElementLayout();
    }

    public void FixCustomElementLayout()
    {
        RectTransform rectTransform = GetRectTransform();
        if (rectTransform != null)
        {
            LayoutElement layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }
            
            RectMask2D rectMask = GetComponent<RectMask2D>();
            if (rectMask == null)
            {
                rectMask = gameObject.AddComponent<RectMask2D>();
            }
            rectMask.enabled = true;
        }
    }

    private void InitializeDay()
    {
        _currentDay = DayOfWeek.Monday;
        if (_backgroundImage != null && _dayBackgrounds != null && _dayBackgrounds.Length > 0)
        {
            int dayIndex = GetDayIndex(_currentDay);
            if (dayIndex < _dayBackgrounds.Length && _dayBackgrounds[dayIndex] != null)
            {
                _backgroundImage.sprite = _dayBackgrounds[dayIndex];
            }
        }
    }

    private void AttachButtonHandlers()
    {
        if (_previousDayButton != null)
        {
            _previousDayButton.onClick.RemoveAllListeners(); 
            _previousDayButton.onClick.AddListener(OnPreviousDayClicked);
        }
        if (_nextDayButton != null)
        {
            _nextDayButton.onClick.RemoveAllListeners(); 
            _nextDayButton.onClick.AddListener(OnNextDayClicked);
        }
    }

    private void UpdateUI()
    {
        if (_dayText != null)
        {
            LocalizedString dayName = GetDayName(_currentDay);
            if (dayName.IsEmpty == false)
            {
                dayName.GetLocalizedStringAsync().Completed += (op) =>
                {
                    if (op.IsDone && op.Result != null && _dayText != null)
                    {
                        _dayText.text = op.Result;
                    }
                };
            }
            else
            {
                _dayText.text = _currentDay.ToString();
            }
        }

        if (_previousDayButton != null)
        {
            bool shouldBeInteractable = !IsFirstDay() && !_isAnimating2;
            _previousDayButton.interactable = shouldBeInteractable;
        }
        
        if (_nextDayButton != null)
        {
            bool shouldBeInteractable = !IsLastDay() && !_isAnimating2;
            _nextDayButton.interactable = shouldBeInteractable;
        }

        UpdateLevelStatus();
    }

    private void UpdateLevelStatus()
    {
        if (_levelStatusImage == null)
            return;

        bool isLevelCompleted = GameManager.GetStoryProgress(_currentDay, PlayerType.Rabbit);
        bool hasGoldenCarrot = GameManager.IsGoldenCarrotCollected(_currentDay, PlayerType.Rabbit);

        if (isLevelCompleted && hasGoldenCarrot && _goldenCarrot != null)
        {
            _levelStatusImage.sprite = _goldenCarrot;
            _levelStatusImage.enabled = true;
        }
        else if (isLevelCompleted && _levelComplite != null)
        {
            _levelStatusImage.sprite = _levelComplite;
            _levelStatusImage.enabled = true;
        }
        else
        {
            _levelStatusImage.enabled = false;
        }
    }

    private void OnPreviousDayClicked()
    {
        if (_isAnimating2 || IsFirstDay())
            return;

        DayOfWeek previousDay = GetPreviousDay(_currentDay);
        
        // Additional safety check to prevent invalid day values
        int dayIndex = GetDayIndex(previousDay);
        if (dayIndex < 0 || dayIndex > 6)
        {
            DebugHelper.LogWarning(this, $"Invalid day calculated: {previousDay}, resetting to Saturday");
            previousDay = DayOfWeek.Saturday;
        }
        
        ChangeDay(previousDay, true); 
    }

    private void OnNextDayClicked()
    {
        if (_isAnimating2 || IsLastDay())
            return;

        DayOfWeek nextDay = GetNextDay(_currentDay);
        
        // Additional safety check to prevent invalid day values
        int dayIndex = GetDayIndex(nextDay);
        if (dayIndex < 0 || dayIndex > 6)
        {
            DebugHelper.LogWarning(this, $"Invalid day calculated: {nextDay}, resetting to Sunday");
            nextDay = DayOfWeek.Sunday;
        }
        
        ChangeDay(nextDay, false); 
    }

    private DayOfWeek GetPreviousDay(DayOfWeek day)
    {
        // Monday -> Sunday, Tuesday -> Monday, ..., Sunday -> Saturday
        if (day == DayOfWeek.Monday)
            return DayOfWeek.Sunday;
        if (day == DayOfWeek.Sunday)
            return DayOfWeek.Saturday;
        return day - 1;
    }

    private DayOfWeek GetNextDay(DayOfWeek day)
    {
        // Sunday -> Monday, Monday -> Tuesday, ..., Saturday -> Sunday
        if (day == DayOfWeek.Sunday)
            return DayOfWeek.Monday;
        if (day == DayOfWeek.Saturday)
            return DayOfWeek.Sunday;
        return day + 1;
    }

    private void ChangeDay(DayOfWeek newDay, bool slideLeft)
    {
        if (_isAnimating2)
            return;

        // Validate that the new day is within valid range (Monday to Sunday)
        int dayIndex = GetDayIndex(newDay);
        if (dayIndex < 0 || dayIndex > 6)
        {
            DebugHelper.LogWarning(this, $"Attempted to set invalid day: {newDay}, keeping current day: {_currentDay}");
            return;
        }

        _currentDay = newDay;
        StartCoroutine(AnimateBackgroundChange(slideLeft));
    }

    private IEnumerator AnimateBackgroundChange(bool slideLeft)
    {
        _isAnimating2 = true;
        UpdateUI();

        if (_backgroundImage == null || _dayBackgrounds == null || _dayBackgrounds.Length == 0)
        {
            _isAnimating2 = false;
            UpdateUI();
            yield break;
        }

        RectTransform backgroundRect = _backgroundImage.GetComponent<RectTransform>();
        if (backgroundRect == null)
        {
            _isAnimating2 = false;
            UpdateUI();
            yield break;
        }

        int currentDayIndex = GetDayIndex(_currentDay);
        if (currentDayIndex >= _dayBackgrounds.Length || _dayBackgrounds[currentDayIndex] == null)
        {
            _isAnimating2 = false;
            UpdateUI();
            yield break;
        }

        GameObject newImageObject = new GameObject("TempBackground");
        newImageObject.transform.SetParent(backgroundRect.parent, false);
        RectTransform newRect = newImageObject.AddComponent<RectTransform>();
        Image newImage = newImageObject.AddComponent<Image>();
        
        newRect.anchorMin = backgroundRect.anchorMin;
        newRect.anchorMax = backgroundRect.anchorMax;
        newRect.sizeDelta = backgroundRect.sizeDelta;
        newRect.anchoredPosition = backgroundRect.anchoredPosition;
        newImage.sprite = _dayBackgrounds[currentDayIndex];
        newImage.color = _backgroundImage.color;

        float screenWidth = backgroundRect.rect.width;
        Vector2 startPos = backgroundRect.anchoredPosition;
        Vector2 newImageStartPos = startPos;
        Vector2 newImageEndPos = startPos;
        Vector2 oldImageEndPos = startPos;

        if (slideLeft)
        {
            newImageStartPos.x = startPos.x - screenWidth;
            newImageEndPos.x = startPos.x;
            oldImageEndPos.x = startPos.x + screenWidth;
        }
        else
        {
            newImageStartPos.x = startPos.x + screenWidth;
            newImageEndPos.x = startPos.x;
            oldImageEndPos.x = startPos.x - screenWidth;
        }

        newRect.anchoredPosition = newImageStartPos;

        float timer = 0f;
        while (timer < _slideAnimationDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / _slideAnimationDuration);
            float easedProgress = EaseInOutQuad(progress);

            backgroundRect.anchoredPosition = Vector2.Lerp(startPos, oldImageEndPos, easedProgress);
            newRect.anchoredPosition = Vector2.Lerp(newImageStartPos, newImageEndPos, easedProgress);

            yield return null;
        }

        _backgroundImage.sprite = _dayBackgrounds[currentDayIndex];
        backgroundRect.anchoredPosition = startPos;

        Destroy(newImageObject);

        _isAnimating2 = false;
        UpdateUI();
    }

    private float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
    }
}

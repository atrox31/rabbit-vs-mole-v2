using RabbitVsMole;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace GameSystems.Story
{
    public abstract class StoryProgressManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] protected GameObject questListPanel;
        [SerializeField] protected TextMeshProUGUI questTextPanel;

        [Header("Timing")]
        [SerializeField] protected float initialDelay = 5f;
        [SerializeField] protected float delayBetweenQuests = 5f;
        [SerializeField] protected float textRefreshInterval = 0.5f;

        [Header("Panel Animation")]
        [SerializeField] protected bool enablePanelAnimation = true;
        [SerializeField] protected float panelSlideDistance = 300f;
        [SerializeField] protected float panelSlideDuration = 0.5f;
        [SerializeField] protected AnimationCurve panelSlideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Typewriter Effect")]
        [SerializeField] protected bool enableTypewriter = true;
        [SerializeField] protected float typewriterSpeed = 30f;
        [SerializeField] protected float typewriterStartDelay = 0.2f;
        [SerializeField] protected bool typewriterSkipTags = true;

        [Header("Sound Effects (via AudioManager)")]
        [SerializeField] protected AudioClip sfxNewTask;
        [SerializeField] protected AudioClip sfxNewStep;
        [SerializeField] protected AudioClip sfxProgressUpdate;
        [SerializeField] protected AudioClip sfxTaskCompleted;
        [SerializeField] protected AudioClip sfxQuestCompleted;
        [SerializeField] protected AudioClip sfxQuestFailed;
        [SerializeField] protected AudioClip sfxTypewriter;
        [SerializeField, Range(0f, 1f)] protected float sfxVolume = 0.8f;

        // Text formatting schemas
        private const string SCHEMA_TITLE = "<style=Title>{0}</style><br>";
        private const string SCHEMA_DESC_COMPLETED = "<s>{0}</s>";
        private const string SCHEMA_DESC_CURRENT = "{0}";
        private const string SCHEMA_TIPS = "<br><br><i>{0}</i>";

        // Localization key schemas
        private const string KEY_NAME = "quest_name_{0}_{1}";
        private const string KEY_DESC = "quest_desc_{0}_{1}_{2}";
        private const string KEY_TIPS = "quest_tips_{0}_{1}_{2}";

        protected readonly Queue<QuestTask> questTaskQueue = new();
        private readonly StringBuilder stringBuilder = new();
        private readonly List<string> completedStepsInCurrentTask = new();

        private int currentTaskId = -1;
        private int lastStepId = -1;
        private QuestTask currentQuest;
        private int lastProgressValue = -1;

        // Cached text parts for incremental updates
        private string cachedTitleText = "";
        private string cachedCompletedStepsText = "";
        private string cachedCurrentDescBase = "";
        private string cachedCurrentDescFormatted = ""; // Formatted with progress for strikethrough
        private string cachedTipsText = "";

        private RectTransform panelRectTransform;
        private Vector2 panelTargetPosition;
        private Vector2 panelHiddenPosition;
        private Coroutine typewriterCoroutine;
        private Coroutine panelAnimationCoroutine;
        private bool isPanelVisible;
        private string currentFullText;
        private bool isTypewriterRunning;

        protected abstract int QuestId { get; }
        protected abstract void BuildQuestQueue();

        protected virtual void Start()
        {
            InitializePanelAnimation();
            BuildQuestQueue();
            
            if (questTaskQueue.Count > 0)
                StartCoroutine(QuestProgressCoroutine());
        }

        #region Audio

        private void PlaySound(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;
            AudioManager.PlaySoundUI(clip, sfxVolume * volumeScale);
        }

        private void PlayNewTaskSound() => PlaySound(sfxNewTask);
        private void PlayNewStepSound() => PlaySound(sfxNewStep);
        private void PlayProgressUpdateSound() => PlaySound(sfxProgressUpdate);
        private void PlayTaskCompletedSound() => PlaySound(sfxTaskCompleted);
        private void PlayQuestCompletedSound() => PlaySound(sfxQuestCompleted);
        private void PlayQuestFailedSound() => PlaySound(sfxQuestFailed);

        #endregion

        #region Panel Animation

        private void InitializePanelAnimation()
        {
            if (questListPanel == null) return;

            panelRectTransform = questListPanel.GetComponent<RectTransform>();
            if (panelRectTransform == null) return;

            panelTargetPosition = panelRectTransform.anchoredPosition;
            panelHiddenPosition = panelTargetPosition + Vector2.up * panelSlideDistance;

            if (enablePanelAnimation)
            {
                panelRectTransform.anchoredPosition = panelHiddenPosition;
                questListPanel.SetActive(false);
                isPanelVisible = false;
            }
        }

        protected void ShowPanel()
        {
            if (!enablePanelAnimation || panelRectTransform == null)
            {
                if (questListPanel != null)
                    questListPanel.SetActive(true);
                return;
            }

            if (isPanelVisible) return;

            if (panelAnimationCoroutine != null)
                StopCoroutine(panelAnimationCoroutine);

            panelAnimationCoroutine = StartCoroutine(AnimatePanelSlide(panelHiddenPosition, panelTargetPosition, true));
        }

        protected void HidePanel()
        {
            if (!enablePanelAnimation || panelRectTransform == null)
            {
                if (questListPanel != null)
                    questListPanel.SetActive(false);
                return;
            }

            if (!isPanelVisible) return;

            if (panelAnimationCoroutine != null)
                StopCoroutine(panelAnimationCoroutine);

            panelAnimationCoroutine = StartCoroutine(AnimatePanelSlide(panelTargetPosition, panelHiddenPosition, false));
        }

        private IEnumerator AnimatePanelSlide(Vector2 from, Vector2 to, bool showAtEnd)
        {
            if (showAtEnd)
                questListPanel.SetActive(true);

            float elapsed = 0f;
            
            while (elapsed < panelSlideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / panelSlideDuration);
                float curveValue = panelSlideCurve.Evaluate(t);
                panelRectTransform.anchoredPosition = Vector2.Lerp(from, to, curveValue);
                yield return null;
            }

            panelRectTransform.anchoredPosition = to;
            isPanelVisible = showAtEnd;

            if (!showAtEnd)
                questListPanel.SetActive(false);

            panelAnimationCoroutine = null;
        }

        #endregion

        #region Typewriter Effect

        private void StartTypewriter(string fullText)
        {
            if (typewriterCoroutine != null)
                StopCoroutine(typewriterCoroutine);

            currentFullText = fullText;

            if (!enableTypewriter || questTextPanel == null)
            {
                questTextPanel.text = fullText;
                return;
            }

            typewriterCoroutine = StartCoroutine(TypewriterCoroutine(fullText));
        }

        private IEnumerator TypewriterCoroutine(string fullText)
        {
            isTypewriterRunning = true;
            questTextPanel.text = "";

            yield return new WaitForSeconds(typewriterStartDelay);

            int visibleCharCount = 0;
            int totalVisibleChars = CountVisibleCharacters(fullText);
            float timeBetweenChars = 1f / typewriterSpeed;
            float timer = 0f;

            questTextPanel.text = fullText;
            questTextPanel.maxVisibleCharacters = 0;

            while (visibleCharCount < totalVisibleChars)
            {
                timer += Time.deltaTime;

                while (timer >= timeBetweenChars && visibleCharCount < totalVisibleChars)
                {
                    timer -= timeBetweenChars;
                    visibleCharCount++;
                    questTextPanel.maxVisibleCharacters = visibleCharCount;

                    if (sfxTypewriter != null && visibleCharCount % 3 == 0)
                        PlaySound(sfxTypewriter, 0.5f);
                }

                yield return null;
            }

            questTextPanel.maxVisibleCharacters = int.MaxValue;
            isTypewriterRunning = false;
            typewriterCoroutine = null;
        }

        private int CountVisibleCharacters(string text)
        {
            if (!typewriterSkipTags)
                return text.Length;

            int count = 0;
            bool inTag = false;

            foreach (char c in text)
            {
                if (c == '<') inTag = true;
                else if (c == '>') inTag = false;
                else if (!inTag) count++;
            }

            return count;
        }

        protected void SkipTypewriter()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }

            if (questTextPanel != null && currentFullText != null)
            {
                questTextPanel.text = currentFullText;
                questTextPanel.maxVisibleCharacters = int.MaxValue;
            }

            isTypewriterRunning = false;
        }

        private void UpdateTextInstant(string text)
        {
            if (text == currentFullText) return; // Skip if text unchanged
            
            currentFullText = text;
            if (questTextPanel != null)
            {
                questTextPanel.text = text;
                questTextPanel.maxVisibleCharacters = int.MaxValue;
            }
        }

        #endregion

        #region Localization

        private string GetLocalizedString(string key) =>
            new LocalizedString("QuestTable", key).GetLocalizedString();

        private string TryGetLocalizedString(string key)
        {
            var result = GetLocalizedString(key);
            return string.IsNullOrEmpty(result) || result == key ? null : result;
        }

        private string FormatWithProgress(string text, QuestTask task)
        {
            if (string.IsNullOrEmpty(text) || task?.ProgressProvider == null)
                return text;

            try
            {
                var (current, total) = task.ProgressProvider();
                return string.Format(text, current, total);
            }
            catch (FormatException)
            {
                return text;
            }
        }

        #endregion

        #region Quest Text Building

        private enum UpdateType { Full, NewStep, ProgressOnly }

        protected void UpdateQuestText(QuestTask task, bool useTypewriter = true, bool isProgressRefresh = false)
        {
            bool isNewTask = currentTaskId != task.TaskId;
            bool isNewStep = !isNewTask && lastStepId != -1 && lastStepId < task.StepId;

            UpdateType updateType = isNewTask ? UpdateType.Full 
                                  : isNewStep ? UpdateType.NewStep 
                                  : UpdateType.ProgressOnly;

            // Skip progress-only updates while typewriter is still animating
            if (updateType == UpdateType.ProgressOnly && isTypewriterRunning)
            {
                HandleProgressUpdate(task); // Still track progress for sounds
                return;
            }

            switch (updateType)
            {
                case UpdateType.Full:
                    HandleNewTask(task);
                    break;
                case UpdateType.NewStep:
                    HandleNewStep(task);
                    break;
                case UpdateType.ProgressOnly:
                    HandleProgressUpdate(task);
                    break;
            }

            lastStepId = task.StepId;

            string finalText = BuildFinalText(task);

            if (updateType == UpdateType.Full && useTypewriter && !isTypewriterRunning)
                StartTypewriter(finalText);
            else if (updateType == UpdateType.NewStep && useTypewriter && !isTypewriterRunning)
                StartTypewriterForNewContent(finalText);
            else
                UpdateTextInstant(finalText);
        }

        private void HandleNewTask(QuestTask task)
        {
            completedStepsInCurrentTask.Clear();
            currentTaskId = task.TaskId;
            lastStepId = -1;
            lastProgressValue = -1;

            // Cache title
            string title = TryGetLocalizedString(string.Format(KEY_NAME, QuestId, task.TaskId));
            cachedTitleText = title != null ? string.Format(SCHEMA_TITLE, title) : "";
            cachedCompletedStepsText = "";

            // Cache current description base (without progress formatting)
            cachedCurrentDescBase = TryGetLocalizedString(string.Format(KEY_DESC, QuestId, task.TaskId, task.StepId)) ?? "";
            cachedCurrentDescFormatted = "";

            // Cache tips
            string tips = TryGetLocalizedString(string.Format(KEY_TIPS, QuestId, task.TaskId, task.StepId));
            cachedTipsText = tips != null ? string.Format(SCHEMA_TIPS, tips) : "";

            PlayNewTaskSound();
        }

        private void HandleNewStep(QuestTask task)
        {
            // Use cached formatted description (with progress values) for strikethrough
            if (!string.IsNullOrEmpty(cachedCurrentDescFormatted))
            {
                string completedLine = string.Format(SCHEMA_DESC_COMPLETED, cachedCurrentDescFormatted);
                completedStepsInCurrentTask.Add(completedLine);
                
                // Rebuild completed steps cache
                stringBuilder.Clear();
                foreach (string step in completedStepsInCurrentTask)
                    stringBuilder.AppendLine(step);
                cachedCompletedStepsText = stringBuilder.ToString();
            }

            lastProgressValue = -1;

            // Cache new current description base
            cachedCurrentDescBase = TryGetLocalizedString(string.Format(KEY_DESC, QuestId, task.TaskId, task.StepId)) ?? "";
            cachedCurrentDescFormatted = "";

            // Cache new tips
            string tips = TryGetLocalizedString(string.Format(KEY_TIPS, QuestId, task.TaskId, task.StepId));
            cachedTipsText = tips != null ? string.Format(SCHEMA_TIPS, tips) : "";

            PlayNewStepSound();
        }

        private void HandleProgressUpdate(QuestTask task)
        {
            if (task.ProgressProvider == null) return;

            var (current, _) = task.ProgressProvider();
            if (lastProgressValue != -1 && current > lastProgressValue)
                PlayProgressUpdateSound();
            lastProgressValue = current;
        }

        private string BuildFinalText(QuestTask task)
        {
            stringBuilder.Clear();
            
            stringBuilder.Append(cachedTitleText);
            stringBuilder.Append(cachedCompletedStepsText);
            
            // Format current description with progress and cache for strikethrough later
            cachedCurrentDescFormatted = FormatWithProgress(cachedCurrentDescBase, task);
            stringBuilder.AppendFormat(SCHEMA_DESC_CURRENT, cachedCurrentDescFormatted);
            
            stringBuilder.Append(cachedTipsText);

            return stringBuilder.ToString();
        }

        private void StartTypewriterForNewContent(string fullText)
        {
            if (typewriterCoroutine != null)
                StopCoroutine(typewriterCoroutine);

            currentFullText = fullText;

            if (!enableTypewriter || questTextPanel == null)
            {
                questTextPanel.text = fullText;
                return;
            }

            // Calculate how many characters were already visible (title + completed)
            int alreadyVisibleChars = CountVisibleCharacters(cachedTitleText + cachedCompletedStepsText);
            
            typewriterCoroutine = StartCoroutine(TypewriterFromPosition(fullText, alreadyVisibleChars));
        }

        private IEnumerator TypewriterFromPosition(string fullText, int startFromChar)
        {
            isTypewriterRunning = true;

            yield return new WaitForSeconds(typewriterStartDelay * 0.5f);

            int visibleCharCount = startFromChar;
            int totalVisibleChars = CountVisibleCharacters(fullText);
            float timeBetweenChars = 1f / typewriterSpeed;
            float timer = 0f;

            questTextPanel.text = fullText;
            questTextPanel.maxVisibleCharacters = visibleCharCount;

            while (visibleCharCount < totalVisibleChars)
            {
                timer += Time.deltaTime;

                while (timer >= timeBetweenChars && visibleCharCount < totalVisibleChars)
                {
                    timer -= timeBetweenChars;
                    visibleCharCount++;
                    questTextPanel.maxVisibleCharacters = visibleCharCount;

                    if (sfxTypewriter != null && visibleCharCount % 3 == 0)
                        PlaySound(sfxTypewriter, 0.5f);
                }

                yield return null;
            }

            questTextPanel.maxVisibleCharacters = int.MaxValue;
            isTypewriterRunning = false;
            typewriterCoroutine = null;
        }

        #endregion

        #region Quest Progress

        private IEnumerator QuestProgressCoroutine()
        {
            DebugHelper.Log(this, $"[Quest] Starting coroutine. Total quests in queue: {questTaskQueue.Count}");
            
            yield return new WaitForSeconds(initialDelay);
            ShowPanel();
            int previousTaskId = -1;
            bool firstShow = true;

            DebugHelper.Log(this, $"[Quest] After initial delay. Quests remaining: {questTaskQueue.Count}");

            while (questTaskQueue.Count > 0)
            {
                if (firstShow)
                    firstShow = false;
                else
                    yield return new WaitForSeconds(delayBetweenQuests);

                currentQuest = questTaskQueue.Dequeue();
                if (currentQuest == null) continue;

                if (previousTaskId != -1 && previousTaskId != currentQuest.TaskId)
                {
                    PlayTaskCompletedSound();
                    yield return new WaitForSeconds(0.3f);
                }

                currentQuest.OnStart?.Invoke();
                DebugHelper.Log(this, $"[Quest] Started: task={currentQuest.TaskId}, step={currentQuest.StepId}, remaining={questTaskQueue.Count}");
                
                if (currentQuest.ProgressProvider != null)
                {
                    var (current, _) = currentQuest.ProgressProvider();
                    lastProgressValue = current;
                }

                UpdateQuestText(currentQuest, useTypewriter: true, isProgressRefresh: false);

                // Wait one frame before checking conditions to ensure OnStart effects are applied
                yield return null;

                float refreshTimer = 0f;

                while (true)
                {
                    if (currentQuest.SuccessCondition?.Invoke() == true)
                    {
                        DebugHelper.Log(this, $"[Quest] Completed: task={currentQuest.TaskId}, step={currentQuest.StepId}");
                        currentQuest.OnEndSuccess?.Invoke();
                        break;
                    }

                    if (currentQuest.FailureCondition?.Invoke() == true)
                    {
                        currentQuest.OnEndFailure?.Invoke();
                        currentQuest = null;
                        StartCoroutine(QuestFailedSequence());
                        yield break;
                    }

                    refreshTimer += Time.deltaTime;
                    if (currentQuest.ProgressProvider != null && refreshTimer >= textRefreshInterval)
                    {
                        refreshTimer = 0f;
                        UpdateQuestText(currentQuest, useTypewriter: false, isProgressRefresh: true);
                    }

                    yield return null;
                }

                previousTaskId = currentQuest.TaskId;
                DebugHelper.Log(this, $"[Quest] Finished task={previousTaskId}. Quests remaining: {questTaskQueue.Count}");
            }

            DebugHelper.Log(this, $"[Quest] While loop exited! Queue count: {questTaskQueue.Count}. Triggering victory...");
            
            PlayTaskCompletedSound();
            yield return new WaitForSeconds(1f);

            currentQuest = null;
            StartCoroutine(QuestCompletedSequence());
        }

        private IEnumerator QuestCompletedSequence()
        {
            DebugHelper.Log(this, $"[StoryProgressManager] All quests completed for Quest {QuestId}");
            
            PlayQuestCompletedSound();
            yield return new WaitForSeconds(1f);
            
            HidePanel();
            yield return new WaitForSeconds(panelSlideDuration + 0.2f);
            
            OnAllQuestsCompleted();
        }

        protected virtual void OnAllQuestsCompleted()
        {
            GameManager.CurrentGameInspector.SinglePlayerVictory();
        }

        private IEnumerator QuestFailedSequence()
        {
            DebugHelper.Log(this, $"[StoryProgressManager] Quest failed for {QuestId}");

            PlayQuestFailedSound();
            yield return new WaitForSeconds(1f);

            HidePanel();
            yield return new WaitForSeconds(panelSlideDuration + 0.2f);
            
            OnQuestFailure();
        }

        protected virtual void OnQuestFailure()
        {
            GameManager.CurrentGameInspector.SinglePlayerDefeat();
        }

        #endregion

        #region Quest Helpers

        private bool IsPlayerIdle() => 
            PlayerAvatar.RabbitStaticInstance == null || !PlayerAvatar.RabbitStaticInstance.IsBusy;

        private Func<bool> WrapWithIdleCheck(Func<bool> condition, bool waitForIdle)
        {
            if (!waitForIdle || condition == null) 
                return condition;
            return () => condition() && IsPlayerIdle();
        }

        protected void AddQuest(int taskId, int stepId, Func<bool> successCondition, 
            Action onStart = null, Action onSuccess = null, 
            Func<bool> failureCondition = null, Action onFailure = null,
            bool waitForIdle = false)
        {
            var quest = new QuestTask(taskId, stepId)
                .WithConditions(WrapWithIdleCheck(successCondition, waitForIdle), failureCondition)
                .WithCallbacks(onStart, onSuccess, onFailure);
            
            questTaskQueue.Enqueue(quest);
            DebugHelper.Log(this, $"[Quest] Added: task={taskId}, step={stepId}. Queue size: {questTaskQueue.Count}");
        }

        protected void AddQuestWithProgress(int taskId, int stepId, 
            Func<int> currentGetter, int total,
            Action onStart = null, Action onSuccess = null,
            Func<bool> failureCondition = null, Action onFailure = null,
            bool waitForIdle = false)
        {
            var quest = new QuestTask(taskId, stepId)
                .WithProgressCounter(currentGetter, total)
                .WithCallbacks(onStart, onSuccess, onFailure);

            if (waitForIdle)
                quest.SuccessCondition = WrapWithIdleCheck(quest.SuccessCondition, true);

            if (failureCondition != null)
                quest.FailureCondition = failureCondition;

            questTaskQueue.Enqueue(quest);
        }

        #endregion
    }
}

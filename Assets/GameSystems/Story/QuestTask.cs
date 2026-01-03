using System;

namespace GameSystems.Story
{
    public class QuestTask
    {
        public int TaskId { get; set; }
        public int StepId { get; set; }
        
        public Func<bool> SuccessCondition { get; set; }
        public Func<bool> FailureCondition { get; set; }
        
        public Action OnStart { get; set; }
        public Action OnEndSuccess { get; set; }
        public Action OnEndFailure { get; set; }

        public Func<(int current, int total)> ProgressProvider { get; set; }

        public QuestTask(int taskId, int stepId)
        {
            TaskId = taskId;
            StepId = stepId;
        }

        public QuestTask WithConditions(Func<bool> success, Func<bool> failure = null)
        {
            SuccessCondition = success;
            FailureCondition = failure;
            return this;
        }

        public QuestTask WithCallbacks(Action onStart = null, Action onSuccess = null, Action onFailure = null)
        {
            OnStart = onStart;
            OnEndSuccess = onSuccess;
            OnEndFailure = onFailure;
            return this;
        }

        public QuestTask WithProgress(Func<(int current, int total)> progressProvider)
        {
            ProgressProvider = progressProvider;
            return this;
        }

        public QuestTask WithProgressCounter(Func<int> currentGetter, int total)
        {
            ProgressProvider = () => (currentGetter(), total);
            SuccessCondition = () => currentGetter() >= total;
            return this;
        }
    }
}

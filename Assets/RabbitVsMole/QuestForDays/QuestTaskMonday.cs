using DialogueSystem;
using GameSystems.Story;
using RabbitVsMole.InteractableGameObject.Base;
using RabbitVsMole.InteractableGameObject.Field;
using RabbitVsMole.InteractableGameObject.Field.Base;
using RabbitVsMole.InteractableGameObject.Storages;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RabbitVsMole.QuestForDays
{
    public class QuestTaskMonday : StoryProgressManager
    {
        protected override int QuestId => 0;
        
        [Header("Actors")]
        [SerializeField] private Actor rabbitActor;

        [Header("Trigger Zones")]
        [SerializeField] private CinematicTriggerZone triggerReachedFarm;
        [SerializeField] private CinematicTriggerZone triggerReachedFarmField;
        [SerializeField] private CinematicTriggerZone triggerWentHome;

        // Stałe dla tego questa
        private const int REQUIRED_SEEDS = 3;
        private const int REQUIRED_CARROTS = 3;

        // Tracking enabled triggers with activation frame to detect actual player interaction
        private readonly Dictionary<CinematicTriggerZone, int> triggerActivationFrame = new();

        protected override void Start()
        {
            DisableAllGameplay();
            DisableTrigger(triggerReachedFarm);
            DisableTrigger(triggerReachedFarmField);
            DisableTrigger(triggerWentHome);
            base.Start();
        }

        protected override void BuildQuestQueue()
        {
            // === TASK 0: Dostań się na farmę ===
            AddQuest(taskId: 0, stepId: 0,
                successCondition: () => WasTriggered(triggerReachedFarm),
                onStart: () =>
                {
                    EnableTrigger(triggerReachedFarm);
                    ShowDialogue("monday_0", "One arm behind back");
                }
            );

            // === TASK 1: Zbierz potrzebne materiały ===
            AddQuest(taskId: 1, stepId: 0,
                successCondition: () => PlayerAvatar.RabbitStaticInstance.Backpack.Seed.IsFull,
                onStart: () =>
                {
                    GameManager.CurrentGameInspector.GameUI.SetInventoryVisible(PlayerType.Rabbit, true);
                    EnableStorages<FarmSeedStorage>();
                    ShowDialogue("monday_2", "Thinking");
                },
                waitForIdle: true
            );

            AddQuest(taskId: 1, stepId: 1,
                successCondition: () => PlayerAvatar.RabbitStaticInstance.Backpack.Water.IsFull,
                onStart: () =>
                {
                    EnableStorages<FarmWaterStorage>();
                    ShowDialogue("monday_3", "Thinking");
                },
                onSuccess: () => ShowDialogue("monday_4", "Thinking"),
                waitForIdle: true
            );
            
            AddQuest(taskId: 1, stepId: 2,
                successCondition: () => WasTriggered(triggerReachedFarmField),
                onStart: () => EnableTrigger(triggerReachedFarmField),
                onSuccess: () => ShowDialogue("monday_5", "Ask")
            );

            // === TASK 2: Wyhoduj marchewki ===
            AddQuestWithProgress(taskId: 2, stepId: 0,
                currentGetter: CountPlantedFields,
                total: REQUIRED_SEEDS,
                onStart: () =>
                {
                    EnableAllFields();
                    EnableStorages<FarmCarrotStorage>();
                    GameManager.CurrentGameStats.SystemAllowToPlantSeed = true;
                },
                onSuccess: () =>
                {
                    ShowDialogue("monday_6", "Ask");
                    GameManager.CurrentGameStats.SystemAllowToWaterField = true;
                },
                waitForIdle: true
            );

            AddQuestWithProgress(taskId: 2, stepId: 1,
                currentGetter: CountWateredFields,
                total: REQUIRED_SEEDS,
                onSuccess: () => ShowDialogue("monday_7", "Stop"),
                waitForIdle: true
            );

            AddQuestWithProgress(taskId: 2, stepId: 2,
                currentGetter: CountReadyOrCollectedCarrots,
                total: REQUIRED_CARROTS,
                onStart: () =>
                {
                    GameManager.CurrentGameStats.SystemAllowToGrowCarrot = true;
                },
                onSuccess: () => ShowDialogue("monday_8", "One arm behind back"),
                waitForIdle: true
            );

            AddQuestWithProgress(taskId: 2, stepId: 3,
                currentGetter: () => GameManager.CurrentGameInspector.RabbitCarrotCount,
                total: REQUIRED_CARROTS,
                onStart: () =>
                {
                    GameManager.CurrentGameStats.SystemAllowToPickCarrot = true;
                },
                onSuccess: () => ShowDialogue("monday_10", "Two arms behind head"),
                waitForIdle: true
            );

            // === TASK 3: Pójdź do domu ===
            AddQuest(taskId: 3, stepId: 0,
                successCondition: () => WasTriggered(triggerWentHome),
                onStart: () =>
                {
                    EnableTrigger(triggerWentHome);
                    ShowDialogue("monday_11", "Stop");
                }
            );
        }

        #region Trigger Zone Helpers

        private const int MIN_FRAMES_BEFORE_TRIGGER = 3;

        private void EnableTrigger(CinematicTriggerZone trigger)
        {
            if (trigger == null) return;
            
            trigger.gameObject.SetActive(true);
            triggerActivationFrame[trigger] = Time.frameCount;
        }

        private void DisableTrigger(CinematicTriggerZone trigger)
        {
            if (trigger != null)
                trigger.gameObject.SetActive(false);
        }

        /// <summary>
        /// Checks if trigger was enabled by quest, waited minimum frames, and then fired
        /// </summary>
        private bool WasTriggered(CinematicTriggerZone trigger)
        {
            if (trigger == null) return false;
            if (!triggerActivationFrame.TryGetValue(trigger, out int activationFrame)) return false;
            
            // Must wait minimum frames to avoid instant trigger when player is already in zone
            int framesSinceActivation = Time.frameCount - activationFrame;
            if (framesSinceActivation < MIN_FRAMES_BEFORE_TRIGGER) return false;
            
            // Trigger must now be inactive (fired and self-deactivated by triggerOnce)
            return !trigger.gameObject.activeSelf;
        }

        #endregion

        #region Helper Methods

        private void ShowDialogue(string dialogueKey, string pose)
        {
            DialogueSystemMain.ShowSimpleDialogue(dialogueKey, rabbitActor, ActorSideOnScreen.Right, pose);
        }

        private void DisableAllGameplay()
        {
            foreach (var field in FindObjectsByType<FarmFieldBase>(FindObjectsSortMode.None))
                field.Active = false;
            foreach (var storage in FindObjectsByType<StorageBase>(FindObjectsSortMode.None))
                storage.Active = false;

            var rules = GameManager.CurrentGameStats;
            rules.SystemShowInventoryOnStart = false;
            rules.SystemAllowToPlantSeed = false;
            rules.SystemAllowToWaterField = false;
            rules.SystemAllowToPickCarrot = false;
            rules.SystemAllowCollapseMound = false;
            rules.SystemAllowToGrowCarrot = false;
            rules.RootsBirthChance = 0;

        }

        private void EnableStorages<T>() where T : StorageBase
        {
            foreach (var storage in FindObjectsByType<T>(FindObjectsSortMode.None))
                storage.Active = true;
        }

        private void EnableAllFields()
        {
            foreach (var field in FindObjectsByType<FarmFieldBase>(FindObjectsSortMode.None))
                field.Active = true;
        }

        #endregion

        #region Progress Counters

        private int CountPlantedFields()
        {
            return FindObjectsByType<FarmFieldBase>(FindObjectsSortMode.None)
                .Count(f => f.State is FarmFieldPlanted);
        }

        private int CountWateredFields()
        {
            return FindObjectsByType<FarmFieldBase>(FindObjectsSortMode.None)
                .Count(f => f.State is FarmFieldWithCarrot);
        }

        private int CountReadyCarrots()
        {
            return FindObjectsByType<FarmFieldBase>(FindObjectsSortMode.None)
                .Count(f => f.IsCarrotReady);
        }

        private int CountReadyOrCollectedCarrots()
        {
            // Ready on field + already collected (in storage or held by player)
            int readyOnField = CountReadyCarrots();
            int collected = GameManager.CurrentGameInspector.RabbitCarrotCount;
            int inHand = PlayerAvatar.RabbitStaticInstance.Backpack.Carrot.Count;
            return readyOnField + collected + inHand;
        }

        #endregion
    }
}

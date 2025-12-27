using Extensions;
using PlayerManagementSystem;
using PlayerManagementSystem.AIBehaviour.Common;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Enums;
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.UI;

namespace RabbitVsMole.InteractableGameObject.Base
{
    public abstract class StorageBase : MonoBehaviour, IInteractableGameObject
    {
        protected abstract void OnCancelAction();
        public abstract bool CanInteract(Backpack backpack);
        public bool Interact(
            [NotNull] PlayerAvatar playerAvatar,
            [NotNull] Func<ActionType, float> OnActionRequested,
            [NotNull] Action OnActionCompleted,
            out Action CancelAction)
        {
            CancelAction = OnCancelAction;
            if (!CanInteract(playerAvatar.Backpack))
                return false;

            if (OnActionRequested == null)
                return false;

            if (OnActionCompleted == null)
                return false;

            return Action(playerAvatar.Backpack, OnActionRequested, OnActionCompleted);
        }

        protected abstract bool Action(Backpack backpack, Func<ActionType, float> OnActionRequested, Action OnActionCompleted);

        protected IEnumerator CompliteAction(Action action, float time)
        {
            var currentTime = 0.0f;
            while (true)
            {
                currentTime += Time.deltaTime;
                if (currentTime >= time)
                {
                    action?.Invoke();
                    yield break;
                }
                yield return null;
            }
        }

        private Outline _outline;
        private Coroutine _outlineCoroutine;
        void Awake()
        {
            gameObject.UpdateTag(AIConsts.SUPPLY_TAG);
            if (!TryGetComponent(out _outline))
            {
                DebugHelper.LogError(this, "You forgot to add component Outline to this interactable!");
                return;
            }
            _outline.enabled = false;
        }

        private const float MIN_WIDTH = 0f;
        private const float MAX_WIDTH = 10f;
        private const float DURATION = 0.5f;

        public void LightUp(PlayerType playerType) => StartEffect(true);
        public void LightDown(PlayerType playerType) => StartEffect(false);

        private void StartEffect(bool increase)
        {
            if (_outline == null) return;

            if (_outlineCoroutine != null)
                StopCoroutine(_outlineCoroutine);

            if (increase)
                _outline.enabled = true;

            _outlineCoroutine = StartCoroutine(AnimateOutline(increase));
        }

        private IEnumerator AnimateOutline(bool increase)
        {
            float startWidth = _outline.OutlineWidth;
            float targetWidth = increase ? MAX_WIDTH : MIN_WIDTH;
            float elapsed = 0f;

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime / DURATION;

                _outline.OutlineWidth = Mathf.Lerp(startWidth, targetWidth, elapsed);

                yield return null;
            }

            if (!increase)
                _outline.enabled = false;

            _outline.OutlineWidth = targetWidth;
            _outlineCoroutine = null;
        }
    }
}
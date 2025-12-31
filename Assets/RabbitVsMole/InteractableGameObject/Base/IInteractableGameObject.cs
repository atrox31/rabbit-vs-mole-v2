using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Enums;
using System;

namespace RabbitVsMole.InteractableGameObject.Base {

    public interface IInteractableGameObject
    {
        public bool CanInteract(Backpack backpack);
        public bool Interact(PlayerAvatar playerAvatar, Func<ActionType, float> OnActionRequested, Action OnActionCompleted, out Action<Action> CancelAction);
        public void LightUp(PlayerType playerType);
        public void LightDown(PlayerType playerType);
    }
}
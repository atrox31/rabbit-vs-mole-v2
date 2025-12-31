using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Base;
using RabbitVsMole.InteractableGameObject.Enums;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System;

namespace RabbitVsMole.InteractableGameObject.Field
{
    public abstract class FarmFieldStateBase : FieldState
    {
        protected FarmFieldBase FieldParent => (FarmFieldBase)Parent;

        protected FarmFieldStateBase(FarmFieldBase parent) : base(parent)
        {
        }



        protected virtual bool CanInteractForRabbit(Backpack backpack) => false;
        protected virtual bool CanInteractForMole(Backpack backpack) => false;

        public override bool CanInteract(Backpack backpack)
        {
            return backpack.PlayerType switch
            {
                PlayerType.Rabbit => CanInteractForRabbit(backpack),
                PlayerType.Mole => CanInteractForMole(backpack),
                _ => false,
            };
        }

        protected virtual bool ActionForRabbit(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted) => false;
        protected virtual bool ActionForMole(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted) => false;

        protected override bool Action(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return playerAvatar.PlayerType switch
            {
                PlayerType.Rabbit => ActionForRabbit(playerAvatar, onActionRequested, onActionCompleted),
                PlayerType.Mole => ActionForMole(playerAvatar, onActionRequested, onActionCompleted),
                _ => false,
            };
        }

    }
}


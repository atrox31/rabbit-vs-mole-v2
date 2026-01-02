using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Enums;
using RabbitVsMole.InteractableGameObject.Base;

namespace RabbitVsMole.InteractableGameObject.Field
{
    public class UndergroundFieldMounded : UndergroundFieldStateBase
    {
        public UndergroundFieldMounded(UndergroundFieldBase parent) : base(parent)
        {
        }

        protected override void OnStart()
        {
            AIPriority = GameManager.CurrentGameStats.AIStats.UndergroundFieldMounded;
            FieldParent.DestroyCarrot();
            FieldParent.DestroyWall();

            FieldParent.CreateMound();
        }

        protected override void OnDestroy()
        {
            FieldParent.DestroyMound();
            AudioManager.PlaySound3D("Assets/Audio/Sound/sfx/punch-a-rock-161647.mp3", FieldParent.transform.position);
        }

        protected override bool CanInteractForMole(Backpack backpack) =>
            GameManager.CurrentGameStats.GameRulesMoleCanEnterUndergroundMoundWithCarrotInHand switch
            {
                true => true,
                false => backpack.Carrot.IsEmpty
            };

        protected override bool ActionForMole(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return StandardAction(new InteractionConfig
            {
                ActionType = ActionType.EnterMound,
                //BackpackAction = true,
                //NewFieldStateProvider = null,
                //NewLinkedFieldStateProvider = null,
                OnActionRequested = onActionRequested,
                OnActionStart = () => playerAvatar.MoveToLinkedField(Parent.LinkedField),
                OnActionCompleted = onActionCompleted,
                //FinalValidation = null,
                //OnPreStateChange = null,
                //OnPostStateChange = null
            });
        }
    }
}
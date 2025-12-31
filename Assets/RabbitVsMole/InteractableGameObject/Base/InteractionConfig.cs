using System;
using RabbitVsMole.InteractableGameObject.Enums;

namespace RabbitVsMole.InteractableGameObject.Base
{
    public class InteractionConfig
    {
        public ActionType ActionType { get; set; } = ActionType.None;
        public bool BackpackAction { get; set; } = true;
        public bool DelayedStatusChange { get; set; } = false;

        // Providers
        public Func<FieldState> NewFieldStateProvider { get; set; } = null;
        public Func<FieldState> NewLinkedFieldStateProvider { get; set; } = null;

        // Callbacks
        public Func<ActionType, float> OnActionRequested { get; set; } = null;
        public Action OnActionStart { get; set; } = null;
        public Action OnActionCompleted { get; set; } = null;

        // Logic Phases
        public Func<bool> FinalValidation { get; set; } = null;
        public Action OnPreStateChange { get; set; } = null;
        public Action OnPostStateChange { get; set; } = null;
    }
}

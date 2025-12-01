using System;
using UnityEngine;

namespace DialogueSystem.Trigger
{
    [Serializable]
    public class SetActiveArguments : DialogueTriggerArguments
    {
        public SetActiveArguments() => Name = "Set Active";
        public GameObject Object;
        public bool Visible;
    }

    public class DT_SetActive : DialogueTriggerBase<SetActiveArguments>
    {
        protected override bool ValidateArguments()
        {
            if (Args.Object == null)
            {
                Debug.LogWarning("DT_SetActive: Object is not set.");
                return false;
            }
            return true;
        }

        protected override void ExecuteInternal()
        {
            Args.Object.SetActive(Args.Visible);
        }
    }
}
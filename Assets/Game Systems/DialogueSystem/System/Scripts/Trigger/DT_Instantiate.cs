using System;
using UnityEngine;

namespace DialogueSystem.Trigger
{
    [Serializable]
    public class InstantiateArguments : DialogueTriggerArguments
    {
        public InstantiateArguments() => Name = "Instantiate";
        public GameObject Object;
        public Vector3 Position;
        public Quaternion Rotation = Quaternion.identity;
        public Vector3 Scale = Vector3.one;
    }

    public class DT_Instantiate : DialogueTriggerBase<InstantiateArguments>
    {
        protected override bool ValidateArguments()
        {
            if (Args.Object == null)
            {
                Debug.LogWarning("DT_Instantiate: Object is not set.");
                return false;
            }
            return true;
        }

        protected override void ExecuteInternal()
        {
            var instance = GameObject.Instantiate(Args.Object, Args.Position, Args.Rotation);
            if (instance != null)
            {
                instance.transform.localScale = Args.Scale;
            }
        }
    }
}
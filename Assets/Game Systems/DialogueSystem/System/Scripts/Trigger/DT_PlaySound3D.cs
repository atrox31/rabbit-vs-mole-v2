using System;
using UnityEngine;

namespace DialogueSystem.Trigger
{
    [Serializable]
    public class PlaySound3DArguments : DialogueTriggerArguments
    {
        public PlaySound3DArguments() => Name = "Play Sound 3D";
        public AudioClip Clip;
        public Vector3 Position;
    }

    public class DT_PlaySound3D : DialogueTriggerBase<PlaySound3DArguments>
    {
        protected override bool ValidateArguments()
        {
            if (Args.Clip == null)
            {
                Debug.LogWarning("DT_PlaySound3D: AudioClip is not set.");
                return false;
            }
            return true;
        }

        protected override void ExecuteInternal()
        {
            AudioManager.PlaySound3D(Args.Clip, Args.Position);
        }
    }
}
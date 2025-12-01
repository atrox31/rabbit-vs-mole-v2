using System;
using UnityEngine;

namespace DialogueSystem.Trigger
{
    [Serializable]
    public class PlaySoundArguments : DialogueTriggerArguments
    {
        public PlaySoundArguments() => Name = "Play Sound";
        public AudioClip Clip;
    }

    public class DT_PlaySound : DialogueTriggerBase<PlaySoundArguments>
    {
        protected override bool ValidateArguments()
        {
            if (Args.Clip == null)
            {
                Debug.LogWarning("DT_PlaySound: AudioClip is not set.");
                return false;
            }
            return true;
        }

        protected override void ExecuteInternal()
        {
            AudioManager.PlaySoundUI(Args.Clip);
        }
    }
}
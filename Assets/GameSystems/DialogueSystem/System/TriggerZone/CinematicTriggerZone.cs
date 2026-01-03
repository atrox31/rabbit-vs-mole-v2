using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

namespace DialogueSystem
{
    public class CinematicTriggerZone : MonoBehaviour
    {
        [SerializeField] private DialogueSequence dialogueSequence;

        [SerializeField] private UnityEvent<string> action;
        [SerializeField] private string actionArg;

        [SerializeField] private bool triggerOnce = true;

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if(dialogueSequence != null)
                    DialogueSystemMain.CreateDialogue(dialogueSequence);

                action?.Invoke(actionArg);

                if(triggerOnce)
                    gameObject.SetActive(false);
            }
        }
    }
}
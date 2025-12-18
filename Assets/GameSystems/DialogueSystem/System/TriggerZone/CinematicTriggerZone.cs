using UnityEngine;

namespace DialogueSystem
{
    public class CinematicTriggerZone : MonoBehaviour
    {
        [SerializeField] private DialogueSequence dialogueSequence;
        [SerializeField] private bool triggerOnce = true;
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                DialogueSystemMain.CreateDialogue(dialogueSequence);
                if(triggerOnce)
                    gameObject.SetActive(false);
            }
        }
    }
}
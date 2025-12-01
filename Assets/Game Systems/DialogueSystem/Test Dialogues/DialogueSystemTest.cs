using DialogueSystem;
using UnityEngine;

public class DialogueSystemTest : MonoBehaviour
{
    [SerializeField] private DialogueSequence sequence;
    public bool StartSelectedDialogue()
    {
        if (sequence == null) return Error.Message("DialogueSystemTest: Test failed! First assign DialogueSequence");
        DialogueSystemMain.CreateDialogue(sequence);
        return true;
    }
}

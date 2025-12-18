using DialogueSystem;
using UnityEngine;

public class DialogueSystemTest : MonoBehaviour
{
    [SerializeField] private DialogueSequence sequence;
    public bool StartSelectedDialogue()
    {
        if (sequence == null)
        {
            DebugHelper.LogError(this,"DialogueSystemTest: Test failed! First assign DialogueSequence");
            return false;
        }

        DialogueSystemMain.CreateDialogue(sequence);
        return true;
    }
}

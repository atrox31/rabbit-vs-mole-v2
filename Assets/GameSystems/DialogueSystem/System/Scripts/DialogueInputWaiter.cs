using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueInputWaiter : MonoBehaviour
{
    [SerializeField] private InputActionAsset _dialogueActionsAsset;

    private InputAction _continueAction;

    public void Awake()
    {
        if (_dialogueActionsAsset == null)
        {
            Debug.LogError("DialogueInputWaiter: Dialogue Actions Asset is not assigned.");
            return;
        }

        _continueAction = _dialogueActionsAsset.FindActionMap("Dialogue").FindAction("Continue");

        if (_continueAction == null)
        {
            Debug.LogError("DialogueInputWaiter: 'Continue' action not found.");
            return;
        }

        _continueAction.performed += context => DialogueSystem.DialogueSystemMain.SetKeyWasPressed(true);
        _continueAction.Enable();
    }

    private void OnDestroy()
    {
        if (_continueAction != null)
        {
            _continueAction.performed -= context => DialogueSystem.DialogueSystemMain.SetKeyWasPressed(true);
            _continueAction.Disable(); 
        }
    }
}
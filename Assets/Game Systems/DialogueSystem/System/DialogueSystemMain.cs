using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using DialogueSystem.Nodes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueSystem
{
    public partial class DialogueSystemMain : MonoBehaviour
    { 
        // config
        private const string PATH_TO_VIEW_PREFAB = "Assets/Game Systems/DialogueSystem/DialogueCanvas.prefab";
        private const bool FORCE_POSE = true;
        private const float DEFAULT_TEXT_LETTER_DELAY = 0.013f;
        private const float ACTOR_FADE_IN_DURATION = 1.0f;

        // const - do not change
        private static readonly int SIDE_SIZE = Enum.GetValues(typeof(ActorSideOnScreen)).Length;
        private static readonly Vector3 WORLD_POSITION = new(5000, 5000, 5000);

        // children in hierarhy       
        private Canvas _canvas;                                                         // parent for all above
        private RawImage[] _screenModelRender = new RawImage[SIDE_SIZE];                // render texture on screen
        private Image[] _backgroundCloudImage = new Image[SIDE_SIZE];                   // cloud image, bc for text
        private TextMeshProUGUI[] _dialogueRenderText = new TextMeshProUGUI[SIDE_SIZE]; // text from dialogue lines
        
        // generated
        private RenderTexture[] _textureRT = new RenderTexture[SIDE_SIZE];              // render target for cameras
        private Camera[] _camera = new Camera[SIDE_SIZE];                               // camera to look at model
        private List<Universal.Pair<Actor, GameObject>> _actors = new();                                       // actor from dialogue lines

        // current helper references
        private DialogueSequence _currentSequence; // Reference to the sequence being played
        private DialogueSystem.Nodes.DialogueNode _currentNode; // Reference to the current node being displayed
        private Coroutine _currentCoroutine;
        private TextMeshProUGUI _currentTextMeshProUGUI;
        private Image _currentBackgroundCloudImage;
        private GameObject[] _currentActorOnSide = new GameObject[SIDE_SIZE];

        // action to continue dialogue
        private bool _keyWasPressed = false;

        // Static field to track the currently active dialogue instance.
        // If it is not null, a dialogue is running.
        private static DialogueSystemMain _activeDialogueInstance = null;

        private LayerMask[] _actorLayer = new LayerMask[SIDE_SIZE];

        /// <summary>
        /// Attempts to create and start a new dialogue system.
        /// Fails if another dialogue is currently running.
        /// </summary>
        /// <param name="dialogSequence">The sequence of lines to play.</param>
        /// <returns>True if the dialogue started, false otherwise.</returns>
        public static bool CreateDialogue(DialogueSequence dialogSequence)
        {
            // Check if a dialogue is already active.
            if (_activeDialogueInstance != null)
            {
                // Cannot start a new dialogue while one is already running.
                return false;
            }

            // Create the new GameObject and add the component.
            GameObject dialogueSystemGO = new GameObject("Active Dialogue System");
            DialogueSystemMain newSystem = dialogueSystemGO.AddComponent<DialogueSystemMain>();

            // Set the new instance as the active one.
            _activeDialogueInstance = newSystem;

            // Start the setup and the dialogue routine.
            if (!newSystem.Setup(dialogSequence))
            {
                Destroy(dialogueSystemGO);
                return false;
                // cleanup is included in Setup
            }

            dialogueSystemGO.transform.SetPositionAndRotation(WORLD_POSITION, Quaternion.identity); // move to far far away
            return true;
        }

        private void SetActiveTextRenderer(ActorSideOnScreen side)
        {
            if (_currentTextMeshProUGUI != null) // first time is not assigned
                _currentTextMeshProUGUI.enabled = false;
            _currentTextMeshProUGUI = _dialogueRenderText[side.i()];
            _currentTextMeshProUGUI.text = String.Empty;
            _currentTextMeshProUGUI.enabled = true;
        }

        private void SetActiveTextBackground(ActorSideOnScreen side)
        {
            if (_currentBackgroundCloudImage != null) // first time is not assigned
                _currentBackgroundCloudImage.enabled = false;
            _currentBackgroundCloudImage = _backgroundCloudImage[side.i()];
            _currentBackgroundCloudImage.enabled = true;
        }

        private IEnumerator FadeActorEffect(ActorSideOnScreen side, bool isActorPresent, bool fadeIn)
        {
            float elapsedTime = 0f;

            // Get the color references
            Color modelStartColor = isActorPresent ? _screenModelRender[side.i()].color : Color.white; // Placeholder if null
            Color cloudStartColor = _backgroundCloudImage[side.i()].color;
            Color textStartColor = _dialogueRenderText[side.i()].color;

            modelStartColor.a = 0f;
            cloudStartColor.a = 0f;
            textStartColor.a = 0f; 

            Color modelTargetColor = modelStartColor;
            Color cloudTargetColor = cloudStartColor;
            Color textTargetColor = textStartColor;

            modelTargetColor.a = 1f;
            cloudTargetColor.a = 1f;
            textTargetColor.a = 1f; 

            // Loop until the fade duration is reached
            while (elapsedTime < ACTOR_FADE_IN_DURATION)
            {
                // Calculate the percentage of time elapsed (0.0 to 1.0)
                float t = elapsedTime / ACTOR_FADE_IN_DURATION;

                // Fade the model (only if the actor is present)
                if (isActorPresent)
                {
                    _screenModelRender[side.i()].color = fadeIn ?
                        Color.Lerp(modelStartColor, modelTargetColor, t)
                        : Color.Lerp(modelTargetColor, modelStartColor, t);
                }

                // Fade the text background cloud (always present if text is shown)
                _backgroundCloudImage[side.i()].color = fadeIn ?
                    Color.Lerp(cloudStartColor, cloudTargetColor, t)
                    : Color.Lerp(cloudTargetColor, cloudStartColor, t);

                _dialogueRenderText[side.i()].color = fadeIn ?
                    Color.Lerp(textStartColor, textTargetColor, t)
                    : Color.Lerp(textTargetColor,textStartColor,  t);


                elapsedTime += Time.deltaTime;
                yield return null; // Wait for the next frame
            }

            // Ensure final alpha is exactly 1.0 to prevent rounding issues
            if (isActorPresent)
            {
                _screenModelRender[side.i()].color = fadeIn ?
                    modelTargetColor : modelStartColor;
            }
            _backgroundCloudImage[side.i()].color = fadeIn ? 
                cloudTargetColor : cloudStartColor;
            _dialogueRenderText[side.i()].color = fadeIn ? 
                textTargetColor : textStartColor;
        }
        

        private bool SetActiveActor(ActorSideOnScreen side, Actor actor)
        {
            if (_currentActorOnSide[side.i()] != null)
            {
                _currentActorOnSide[side.i()].SetActive(false);
            }
            var searchedActor = _actors.GetSecondByFirst(actor);
            /* allow null actors
            if(searchedActor == null)
            {
                return Error.Message($"DialogueSystemMain->SetActiveActor: Can not find actor model (instance) for '{actor.name}' on side '{side.ToString()}'");
            }
            */
            _currentActorOnSide[side.i()] = searchedActor;
            _currentActorOnSide[side.i()]?.SetActive(true);
            return true;
        }

        private bool SetClip(Actor actor, string animationClipName)
        {
            Animation animComponent = FindActorGameObject(actor)?.GetComponent<Animation>();
            if (animComponent == null) //return Error.Message($"DialogueSystemMain->SetClip: Can not find Animation component in Actor ({actor.name})");
                return true;

            AnimationClip animationClip = actor.GetPoseClip(animationClipName);
            if(animationClip == null){
                return Error.Message($"DialogueSystemMain->PlayDialogueRoutine->SetClip: AnimationClip ({animationClipName}) not found in Actor({actor.name})");
            }

            if (animComponent.GetClip(animationClipName) == null)
            {
                animComponent.AddClip(animationClip, animationClipName);

                if(FORCE_POSE)
                    animComponent[animationClipName].speed = 0;
            }
            animComponent[animationClipName].time = 0;

            animComponent.Play(animationClipName);
            return true;
        }

        public IEnumerator WaitForContinue()
        {
            _keyWasPressed = false;
            yield return new WaitUntil(() => _keyWasPressed);
        }

        private IEnumerator PlayDialogueRoutine()
        {
            Debug.Log("Dialogue sequence started.");
            bool[] hasAppeared = new bool[Enum.GetValues(typeof(ActorSideOnScreen)).Length];

            // Always start from StartNode
            if (_currentSequence.StartNode == null)
            {
                Debug.LogError("DialogueSystemMain->PlayDialogueRoutine: StartNode is missing.");
                Cleanup();
                Destroy(gameObject);
                yield break;
            }

            // Get starting node from StartNode's first exit port
            object currentNode = null;
            if (_currentSequence.StartNode.ExitPorts != null && _currentSequence.StartNode.ExitPorts.Count > 0)
            {
                string firstNodeGUID = _currentSequence.StartNode.ExitPorts[0].TargetNodeGUID;
                currentNode = _currentSequence.GetNodeByGUID(firstNodeGUID);
            }

            if (currentNode == null)
            {
                Debug.LogWarning("DialogueSystemMain->PlayDialogueRoutine: StartNode has no connections. Ending dialogue.");
                Cleanup();
                Destroy(gameObject);
                yield break;
            }

            // --- DIALOGUE PLAYBACK LOGIC ---
            while (currentNode != null)
            {
                // Handle TriggerNode
                if (currentNode is TriggerNode triggerNode)
                {
                    // Execute the trigger (pass sequence to resolve data connections)
                    var trigger = triggerNode.GetTrigger(_currentSequence);
                    if (trigger != null)
                    {
                        trigger.Execute();
                        Debug.Log($"Trigger executed: {trigger.GetType().Name}");
                    }
                    else
                    {
                        Debug.LogWarning($"TriggerNode {triggerNode.GUID} has no valid trigger assigned.");
                    }

                    // Move to next node
                    if (triggerNode.ExitPorts.Count > 0)
                    {
                        string nextGUID = triggerNode.ExitPorts[0].TargetNodeGUID;
                        currentNode = _currentSequence.GetNodeByGUID(nextGUID);
                        if (currentNode == null)
                        {
                            Debug.LogError($"DialogueSystemMain: Cannot find next node with GUID: {nextGUID}. Ending dialogue.");
                            currentNode = null;
                        }
                    }
                    else
                    {
                        // End of graph: No exit ports
                        currentNode = null;
                    }
                }
                // Handle LogicNode
                else if (currentNode is LogicNode logicNode)
                {
                    // Resolve condition value
                    bool conditionValue = logicNode.Condition;

                    // Check if condition is connected to a TriggerDataNode
                    if (logicNode.ConditionDataConnection != null)
                    {
                        var dataNode = _currentSequence.GetTriggerDataNodeByGUID(logicNode.ConditionDataConnection.SourceDataNodeGUID);
                        if (dataNode != null)
                        {
                            var value = dataNode.GetOutputValue(logicNode.ConditionDataConnection.SourcePortName, _currentSequence);
                            if (value != null)
                            {
                                try
                                {
                                    // Convert value to bool
                                    if (value is bool boolValue)
                                    {
                                        conditionValue = boolValue;
                                    }
                                    else
                                    {
                                        conditionValue = Convert.ToBoolean(value);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogWarning($"DialogueSystemMain: Failed to convert condition value to bool: {ex.Message}. Using default value: {logicNode.Condition}");
                                }
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"DialogueSystemMain: LogicNode {logicNode.GUID} has ConditionDataConnection but source node not found. Using default value: {logicNode.Condition}");
                        }
                    }

                    Debug.Log($"LogicNode {logicNode.GUID} evaluated condition: {conditionValue}");

                    // Route to appropriate output based on condition
                    List<NodeLink> exitPorts = conditionValue ? logicNode.ExitPortsTrue : logicNode.ExitPortsFalse;
                    
                    if (exitPorts != null && exitPorts.Count > 0)
                    {
                        string nextGUID = exitPorts[0].TargetNodeGUID;
                        currentNode = _currentSequence.GetNodeByGUID(nextGUID);
                        if (currentNode == null)
                        {
                            Debug.LogError($"DialogueSystemMain: Cannot find next node with GUID: {nextGUID} from LogicNode {(conditionValue ? "True" : "False")} output. Ending dialogue.");
                            currentNode = null;
                        }
                    }
                    else
                    {
                        // No exit port for this condition branch
                        Debug.LogWarning($"DialogueSystemMain: LogicNode {logicNode.GUID} has no exit port for {(conditionValue ? "True" : "False")} condition. Ending dialogue.");
                        currentNode = null;
                    }
                }
                // Handle DialogueNode
                else if (currentNode is DialogueNode dialogueNode)
                {
                    // Choice Node (if implemented)
                    if (dialogueNode.ExitPorts.Count > 1)
                    {
                        //TODO: choice's
                    }
                    
                    StartLineForNode(dialogueNode);

                    // slowly show new actor if new
                    if (!hasAppeared[dialogueNode.ScreenPosition.i()])
                    {
                        bool actorIsPresent = dialogueNode._actor != null;
                        yield return StartCoroutine(FadeActorEffect(dialogueNode.ScreenPosition, actorIsPresent, true));
                        hasAppeared[dialogueNode.ScreenPosition.i()] = actorIsPresent;
                    }

                    // normal dialogue line show time
                    yield return StartCoroutine(TypeText(dialogueNode.text, dialogueNode.ScreenPosition));
                    yield return WaitForContinue();

                    if (dialogueNode.ExitPorts.Count > 0)
                    {
                        string nextGUID = dialogueNode.ExitPorts[0].TargetNodeGUID;
                        currentNode = _currentSequence.GetNodeByGUID(nextGUID);
                        if (currentNode == null)
                        {
                            // Error: Next node not found (badly connected graph)
                            Debug.LogError($"DialogueSystemMain: Cannot find next node with GUID: {nextGUID}. Ending dialogue.");
                            currentNode = null;
                        }
                    }
                    else
                    {
                        // End of graph: No exit ports
                        currentNode = null;
                    }
                }
                else
                {
                    // Unknown node type
                    Debug.LogError($"DialogueSystemMain: Unknown node type: {currentNode.GetType().Name}. Ending dialogue.");
                    currentNode = null;
                }
            }
            
            // fade out
            foreach (ActorSideOnScreen side in Enum.GetValues(typeof(ActorSideOnScreen)))
            {
                StartCoroutine(FadeActorEffect(side, hasAppeared[side.i()], false));
            }
            yield return new WaitForSeconds(ACTOR_FADE_IN_DURATION);

            // --- DIALOGUE ENDING ---
            Debug.Log("Dialogue sequence finished.");
            Cleanup();
            Destroy(gameObject);
        }

        void StartLineForNode(DialogueSystem.Nodes.DialogueNode node)
        {
            SetActiveActor(node.ScreenPosition, node._actor);
            SetClip(node._actor, node._poseName);
            SetActiveTextRenderer(node.ScreenPosition);
            SetActiveTextBackground(node.ScreenPosition);
        }

        IEnumerator TypeText(string fullText, ActorSideOnScreen side)
        {
            _currentTextMeshProUGUI.text = String.Empty;
            _keyWasPressed = false; // prevent ship all text
            
            // Use StringBuilder to avoid string concatenation allocations
            StringBuilder textBuilder = new StringBuilder(fullText.Length);
            int textLength = fullText.Length;
            
            for (int i = 0; i < textLength; i++)
            {
                textBuilder.Append(fullText[i]);
                _currentTextMeshProUGUI.text = textBuilder.ToString();
                if (_keyWasPressed)
                {
                    _currentTextMeshProUGUI.text = fullText;
                    _keyWasPressed = false;
                    yield break;
                }

                // Opcjonalnie: odtwarzaj d�wi�k pisania
                yield return new WaitForSeconds(DEFAULT_TEXT_LETTER_DELAY);
            }
        }
        public static void SetKeyWasPressed(bool state)
        {
            _activeDialogueInstance._keyWasPressed = state;
        }

        private bool Cleanup()
        {
            foreach (ActorSideOnScreen side in Enum.GetValues(typeof(ActorSideOnScreen)))
            {
                _textureRT[side.i()]?.Release();
                Destroy(_textureRT[side.i()]);
            }

            if(_canvas != null)
            {
                Destroy(_canvas.gameObject);
                _canvas = null;
            }

            // CRITICAL: Clear the static reference so a new dialogue can start.
            _activeDialogueInstance = null;
            return false; // for setup succes failure
        }

        private void OnDestroy()
        {
            // Fallback safety: Ensure the static reference is cleared if the object is destroyed unexpectedly.
            if (_activeDialogueInstance == this)
            {
                _activeDialogueInstance = null;
            }
        }
    }
}

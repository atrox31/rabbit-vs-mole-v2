
using Extensions;
using GameSystems;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DialogueSystem
{
    partial class DialogueSystemMain
    {
        private bool LoadCanvasPrefab(Transform parrent)
        {
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(PATH_TO_VIEW_PREFAB);
            bool succes = false;

            handle.Completed += (op) =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject loadedPrefab = op.Result;
                    Instantiate(loadedPrefab, Vector3.zero, Quaternion.identity, _activeDialogueInstance.transform);
                    DebugHelper.Log(this, $"Prefab loaded successfully using Address: {PATH_TO_VIEW_PREFAB}");
                    Addressables.Release(handle);
                    succes = true;
                }
                else
                {
                    Debug.LogError($"Failed to load asset at address: {PATH_TO_VIEW_PREFAB}");
                    Debug.LogError($"Tip: Try to change PATH_TO_VIEW_PREFAB");
                    succes = false;
                }
            };

            handle.WaitForCompletion();
            return succes;
        }
        private T FindAndAssignComponent<T>(Transform parent, string childName, string context, ref T targetField) where T : Component
        {
            var child = parent.Find(childName);
            if (child == null)
            {
                Debug.LogError($"{context}: Can not find '{childName}' in '{parent.name}' transform.");
                return null;
            }

            T component = child.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError($"{context}: Can not find '{typeof(T).Name}' component on '{childName}'.");
                return null;
            }

            targetField = component;
            return component;
        }

        private bool FindAllRequiredComponents()
        {
            const string setupContext = "DialogueSystemMain->Setup";

            _canvas = GetComponentInChildren<Canvas>();
            if (_canvas == null)
            {
                DebugHelper.LogError(this, $"{setupContext}: Can not find '_canvas' in children");
                return false;
            }

            foreach (ActorSideOnScreen side in Enum.GetValues(typeof(ActorSideOnScreen)))
            {
                string sideName = side.ToString();
                
                if (FindAndAssignComponent(_canvas.transform, $"{sideName}Model", setupContext, ref _screenModelRender[side.i()]) == null)
                {
                    return false;
                }

                if (FindAndAssignComponent(_screenModelRender[side.i()].transform, $"{sideName}TextBackground", setupContext, ref _backgroundCloudImage[side.i()]) == null)
                {
                    return false;
                }
                _backgroundCloudImage[side.i()].enabled = false;

                if (FindAndAssignComponent(_backgroundCloudImage[side.i()].transform, $"{sideName}Text", setupContext, ref _dialogueRenderText[side.i()]) == null)
                {
                    return false;
                }
                _dialogueRenderText[side.i()].enabled = false;
            }

            return true;
        }

        private bool Setup(DialogueSequence sequenceToPlay)
        {
            if (sequenceToPlay == null)
            {
                DebugHelper.LogError(this, "DialogueSystemMain->Setup: DialogueSequence sequenceToPlay == null");
                return false;
            }
            
            _currentSequence = sequenceToPlay;

            foreach (ActorSideOnScreen side in Enum.GetValues(typeof(ActorSideOnScreen)))
            {
                int layerIndex = LayerMask.NameToLayer($"DialogueRender{side.ToString()}");
                if (layerIndex == -1)
                {
                    DebugHelper.LogError(this, $"Layer not found for side: {side.ToString()}.");
                    return false;
                }
                _actorLayer[side.i()] = layerIndex;
            }

            // load canvas
            if (!LoadCanvasPrefab(transform)) return Cleanup();
            // assign all required assets
            if (!FindAllRequiredComponents()) return Cleanup();
            // create reder target textures
            if (!CreateRenderTargetTextures()) return Cleanup();
            // assign render target textures to images on screen
            if (!AssignRenderTargetTextures()) return Cleanup();
            // create camera objects
            if (!CreateCameraObjects()) return Cleanup();
            // create gameobjects (actors)
            if (!PrepareActorModels()) return Cleanup();

            HideBackgroundImageAndText();

            // Freeze all player avatars during dialogue
            EventBus.Publish(new DialogueFreezeEvent(true));

            DebugHelper.Log(this, $"Dialogue System created for sequence: {sequenceToPlay.name}");
            _currentCoroutine = StartCoroutine(PlayDialogueRoutine());
            return true;
        }

        private void HideBackgroundImageAndText()
        {
            foreach (ActorSideOnScreen side in Enum.GetValues(typeof(ActorSideOnScreen)))
            {
                _backgroundCloudImage[side.i()].enabled = false;
                _dialogueRenderText[side.i()].enabled = false;
            }
        }

        private bool AssignRenderTargetTextures()
        {
            foreach (ActorSideOnScreen side in Enum.GetValues(typeof(ActorSideOnScreen)))
            {
                if ((_screenModelRender[side.i()].texture = _textureRT[side.i()]) == null)
                {
                    DebugHelper.LogError(this, "DialogueSystemMain->Setup->AssignRenderTargetTextures: cannot assign texture render target to screen");
                    return false;
                }
            }
            return true;
        }

        private bool PrepareActorModels()
        {
            int lineNumberForDebugErrorMessage = -1;
            foreach (var dialogueLine in _currentSequence.NodeMap.Values)
            {
                lineNumberForDebugErrorMessage++;
                if (IsActorPrepared(dialogueLine._actor)) continue;

                if (dialogueLine._actor == null)
                {
                    DebugHelper.LogWarning(this, $"DialogueSystemMain->Setup->PrepareActorModels: null actor for line: {lineNumberForDebugErrorMessage.ToString()}");
                    _actors.Add(new Universal.Pair<Actor, GameObject>(dialogueLine._actor, null));
                    continue;
                }

                if (dialogueLine._actor.actorModel == null)
                {
                    DebugHelper.LogError(this, $"DialogueSystemMain->Setup->PrepareActorModels: cannot find actor model in '{dialogueLine._actor.name}'");
                    return false;
                }
                
                var actor = Instantiate(
                    dialogueLine._actor.actorModel, 
                    dialogueLine._actor.renderVector3Pose,
                    Quaternion.Euler(dialogueLine._actor.renderVector3Rotation),
                    _activeDialogueInstance.transform);
                actor.transform.localScale = dialogueLine._actor.renderVector3Scale;
                actor.transform.SetLayerRecursively(_actorLayer[dialogueLine.ScreenPosition.i()]);
                actor.AddComponent<Animation>();
                actor.SetActive(false);

                _actors.Add(new Universal.Pair<Actor, GameObject>(dialogueLine._actor, actor));
            }

            return (_actors.Count > 0);
        }

        private GameObject FindActorGameObject(Actor actor)
        {
            foreach (var pair in _actors)
            {
                if (pair.First == actor)
                    return pair.Second;
            }
            return null;
        }

        private bool IsActorPrepared(Actor actor)
        {
            if (_actors.Count == 0) return false;
            foreach (var actorModel in _actors)
            {
                if (actorModel.First == actor) return true;
            }
            return false;
        }

        private bool CreateCameraObjects()
        {
            foreach (ActorSideOnScreen side in Enum.GetValues(typeof(ActorSideOnScreen)))
            {
                if (!CreateCamera(side)) return false;
            }
            return true;
        }

        private bool CreateCamera(ActorSideOnScreen side)
        {
            GameObject cameraGO = new GameObject($"ActorCamera_{side.ToString()}");
            cameraGO.transform.parent = transform;
            cameraGO.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            Camera newCamera = cameraGO.AddComponent<Camera>();

            // Configure the camera for actor rendering
            newCamera.targetTexture = _textureRT[side.i()];
            newCamera.orthographic = true;
            newCamera.orthographicSize = 1.5f;                  // Adjust based on actor scale
            newCamera.clearFlags = CameraClearFlags.SolidColor;
            newCamera.backgroundColor = new Color(0, 0, 0, 0);  // Transparent background
            newCamera.cullingMask = 1 << _actorLayer[side.i()];      // Only render objects on the actor's layer
            newCamera.targetDisplay = -1;                       // Ensure it does not render to the main screen

            _camera[side.i()] = newCamera;
            return (newCamera != null);
        }

        private bool CreateRenderTargetTextures()
        {
            const int CAMERA_RESOLUTION = 512;
            return
                CreteTextureTarget(ActorSideOnScreen.Left, CAMERA_RESOLUTION)
                && CreteTextureTarget(ActorSideOnScreen.Right, CAMERA_RESOLUTION);
        }

        private bool CreteTextureTarget(ActorSideOnScreen side, int resolution)
        {
            _textureRT[(int)side]?.Release();
            // to awoid memeory leaks becouse this is unity and shitty c# with "garbage collectr" (tfuu)

            _textureRT[(int)side] = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
            _textureRT[(int)side].name = $"{side.ToString()}_ActorRT";
            return _textureRT[(int)side].Create();
        }

    }
}
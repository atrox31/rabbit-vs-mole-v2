#if UNITY_EDITOR || DEVELOPMENT_BUILD

#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using RabbitVsMole;
using RabbitVsMole.InteractableGameObject.Base;
using RabbitVsMole.InteractableGameObject.Field.Base;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_RENDER_PIPELINE_UNIVERSAL || USING_URP
using UnityEngine.Rendering.Universal;
#endif
#if UNITY_RENDER_PIPELINE_UNIVERSAL || USING_URP
using UnityEngine.Rendering.Universal;
#endif

namespace RabbitVsMole.Debugging
{
    /// <summary>
    /// Global debug helper that survives scene loads and reacts to F1-F4 hotkeys.
    /// </summary>
    public class GlobalDebugHotkeys : MonoBehaviour
    {
        private static GlobalDebugHotkeys _instance;

        private readonly List<Canvas> _disabledCanvases = new List<Canvas>();
        private readonly List<Camera> _disabledCameras = new List<Camera>();

        private bool _showMenu;
        private bool _canvasesHidden;

        private GameObject _freeCamObject;
        private bool _cursorVisible = true;

        private bool _orbitActive;
        private Coroutine _orbitRoutine;
        private float _orbitSpeedDeg = 30f;
        private Vector3 _orbitPivot;
        private Vector3 _orbitOffset;

        private Vector3 _railStartPos;
        private Quaternion _railStartRot;
        private bool _railStartSet;

        private Vector3 _railEndPos;
        private Quaternion _railEndRot;
        private bool _railEndSet;

        private float _railDuration = 5f;
        private bool _railUseSinEase;
        private bool _railAnimating;
        private Coroutine _railRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null)
            {
                return;
            }

            var host = new GameObject(nameof(GlobalDebugHotkeys));
            host.AddComponent<GlobalDebugHotkeys>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (IsKeyDown(Key.F1))
            {
                _showMenu = !_showMenu;
            }

            if (IsKeyDown(Key.F2))
            {
                ToggleCanvases();
            }

            if (IsKeyDown(Key.F3))
            {
                EnableFreeCam();
            }

            if (IsKeyDown(Key.F4))
            {
                RestoreCameras();
            }

            if (IsKeyDown(Key.F5))
            {
                CaptureRailStart();
            }

            if (IsKeyDown(Key.F6))
            {
                CaptureRailEnd();
            }

            if (IsKeyDown(Key.F7))
            {
                StartRailMove();
            }

            if (IsKeyDown(Key.F8))
            {
                ToggleCursor();
            }

            if (IsKeyDown(Key.NumpadPlus) || IsKeyDown(Key.Equals))
            {
                if (_orbitActive)
                {
                    AdjustOrbitSpeed(5f);
                }
                else
                {
                    AdjustRailDuration(1f);
                }
            }

            if (IsKeyDown(Key.NumpadMinus) || IsKeyDown(Key.Minus))
            {
                if (_orbitActive)
                {
                    AdjustOrbitSpeed(-5f);
                }
                else
                {
                    AdjustRailDuration(-1f);
                }
            }

            if (IsKeyDown(Key.NumpadMultiply))
            {
                ToggleRailEasing();
            }

            if (IsKeyDown(Key.F9))
            {
                ToggleOrbit();
            }

            if (IsKeyDown(Key.F10))
            {
                RandomizeFields();
            }

            if (IsKeyDown(Key.F11))
            {
                DebugSteamLobbies();
            }
#else
            if (Input.GetKeyDown(KeyCode.F1))
            {
                _showMenu = !_showMenu;
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                ToggleCanvases();
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                EnableFreeCam();
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                RestoreCameras();
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                CaptureRailStart();
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                CaptureRailEnd();
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                StartRailMove();
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                ToggleCursor();
            }

            if (Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Equals))
            {
                if (_orbitActive)
                {
                    AdjustOrbitSpeed(5f);
                }
                else
                {
                    AdjustRailDuration(1f);
                }
            }

            if (Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus))
            {
                if (_orbitActive)
                {
                    AdjustOrbitSpeed(-5f);
                }
                else
                {
                    AdjustRailDuration(-1f);
                }
            }

            if (Input.GetKeyDown(KeyCode.KeypadMultiply))
            {
                ToggleRailEasing();
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                ToggleOrbit();
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                RandomizeFields();
            }

            if (Input.GetKeyDown(KeyCode.F11))
            {
                DebugSteamLobbies();
            }
#endif
        }

        private void OnGUI()
        {
            // Always draw Steam debug window if active
            DrawSteamDebugWindow();
            
            if (!_showMenu)
            {
                return;
            }

            const float width = 520f;
            const float height = 360f;
            const float margin = 10f;
            GUIStyle style = new GUIStyle();
            style.fontSize = 28;

            GUI.Box(new Rect(margin, margin, width, height), "Debug Hotkeys");
            GUI.Label(
                new Rect(margin + 10f, margin + 25f, width - 20f, height - 35f),
                "F1 - toggle this menu\n" +
                "F2 - toggle all canvases\n" +
                "F3 - disable cameras + FreeCam\n" +
                "F4 - restore cameras, remove FreeCam\n" +
                "F5 - set rail START from active camera (pos/rot)\n" +
                "F6 - set rail END from active camera (pos/rot)\n" +
                "F7 - play rail move (" + _railDuration.ToString("0.0") + "s, " + (_railUseSinEase ? "sinus" : "smooth") + ")\n" +
                "F8 - toggle cursor visibility (" + (_cursorVisible ? "shown" : "hidden") + ")\n" +
                "F9 - toggle orbit around pivot (always look at pivot)\n" +
                "F10 - randomize states of all fields (FarmField water random)\n" +
                "F11 - debug Steam lobbies (list ALL lobbies without filter)\n" +
                "+ / - : adjust rail duration (+/-1s, min 1s) or orbit speed (+/-5 deg/s when orbit on)\n" +
                "* : toggle sinusoidal easing\n" +
                "Rail start set: " + (_railStartSet ? "YES" : "no") + " | Rail end set: " + (_railEndSet ? "YES" : "no") + "\n" +
                (_railAnimating ? "Rail animation: RUNNING" : "Rail animation: idle") + "\n" +
                (_orbitActive ? ("Orbit: ON @ " + _orbitSpeedDeg.ToString("0.0") + " deg/s") : "Orbit: OFF")
                //style
            );
        }

        private void ToggleCanvases()
        {
            if (!_canvasesHidden)
            {
                _disabledCanvases.Clear();
                foreach (var canvas in Resources.FindObjectsOfTypeAll<Canvas>())
                {
                    if (canvas == null || !canvas.gameObject.scene.IsValid())
                    {
                        continue;
                    }

                    if (!canvas.enabled)
                    {
                        continue;
                    }

                    _disabledCanvases.Add(canvas);
                    canvas.enabled = false;
                }

                _canvasesHidden = true;
                return;
            }

            foreach (var canvas in _disabledCanvases)
            {
                if (canvas != null)
                {
                    canvas.enabled = true;
                }
            }

            _disabledCanvases.Clear();
            _canvasesHidden = false;
        }

        private void EnableFreeCam()
        {
            if (_freeCamObject != null)
            {
                return;
            }

            _disabledCameras.Clear();

            Camera reference = null;
            foreach (var cam in Camera.allCameras)
            {
                if (cam == null || !cam.gameObject.scene.IsValid() || !cam.enabled)
                {
                    continue;
                }

                if (reference == null)
                {
                    reference = cam;
                }

                _disabledCameras.Add(cam);
                cam.enabled = false;
            }

            _freeCamObject = new GameObject("DebugFreeCam");
            var camComponent = _freeCamObject.AddComponent<Camera>();
            _freeCamObject.AddComponent<AudioLisnerController>();
            _freeCamObject.AddComponent<DebugFreeCamController>();

            if (reference != null)
            {
                _freeCamObject.transform.SetPositionAndRotation(reference.transform.position, reference.transform.rotation);
                camComponent.fieldOfView = reference.fieldOfView;
                camComponent.cullingMask = reference.cullingMask;
               
            }

            EnsurePostProcessing(_freeCamObject);
            ExcludeOnlyEditorLayer(camComponent);
            DontDestroyOnLoad(_freeCamObject);
        }

        private void RestoreCameras()
        {
            StopRailIfRunning();
            StopOrbitIfRunning();

            if (_freeCamObject != null)
            {
                Destroy(_freeCamObject);
                _freeCamObject = null;
            }

            foreach (var cam in _disabledCameras)
            {
                if (cam != null)
                {
                    cam.enabled = true;
                }
            }

            _disabledCameras.Clear();
        }

#if ENABLE_INPUT_SYSTEM
        private static bool IsKeyDown(Key key)
        {
            return Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
        }
#endif

        private static void ExcludeOnlyEditorLayer(Camera camera)
        {
            var layer = LayerMask.NameToLayer("OnlyEditor");
            if (layer >= 0)
            {
                camera.cullingMask &= ~(1 << layer);
            }
        }

        private static void EnsurePostProcessing(GameObject target)
        {

            var data = target.GetComponent<UniversalAdditionalCameraData>();
            if (data == null)
                data = target.AddComponent<UniversalAdditionalCameraData>();

            data.renderPostProcessing = true; // ensure post process checkbox is on

        }

        private Camera GetActiveCamera()
        {
            if (_freeCamObject != null)
            {
                var cam = _freeCamObject.GetComponent<Camera>();
                if (cam != null)
                {
                    return cam;
                }
            }

            if (Camera.main != null)
            {
                return Camera.main;
            }

            return Camera.allCameras.Length > 0 ? Camera.allCameras[0] : null;
        }

        private void CaptureRailStart()
        {
            var cam = GetActiveCamera();
            if (cam == null)
            {
                return;
            }

            _railStartPos = cam.transform.position;
            _railStartRot = cam.transform.rotation;
            _railStartSet = true;
        }

        private void CaptureRailEnd()
        {
            var cam = GetActiveCamera();
            if (cam == null)
            {
                return;
            }

            _railEndPos = cam.transform.position;
            _railEndRot = cam.transform.rotation;
            _railEndSet = true;
        }

        private void StartRailMove()
        {
            if (_railAnimating || !_railStartSet || !_railEndSet)
            {
                return;
            }

            var cam = GetActiveCamera();
            if (cam == null)
            {
                return;
            }

            StopOrbitIfRunning();
            StopRailIfRunning();
            _railRoutine = StartCoroutine(RailMoveRoutine(cam));
        }

        private void StopRailIfRunning()
        {
            if (_railRoutine != null)
            {
                StopCoroutine(_railRoutine);
                _railRoutine = null;
            }

            _railAnimating = false;
        }

        private IEnumerator RailMoveRoutine(Camera cam)
        {
            _railAnimating = true;

            var controller = cam.GetComponent<DebugFreeCamController>();
            var controllerWasEnabled = controller != null && controller.enabled;
            if (controller != null)
            {
                controller.enabled = false;
            }

            cam.transform.SetPositionAndRotation(_railStartPos, _railStartRot);

            var duration = Mathf.Max(1f, _railDuration);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = _railUseSinEase
                    ? 0.5f - Mathf.Cos(Mathf.PI * t) * 0.5f // sin ease in/out
                    : Mathf.SmoothStep(0f, 1f, t);

                cam.transform.position = Vector3.Lerp(_railStartPos, _railEndPos, eased);
                cam.transform.rotation = Quaternion.Slerp(_railStartRot, _railEndRot, eased);
                yield return null;
            }

            cam.transform.SetPositionAndRotation(_railEndPos, _railEndRot);

            if (controller != null)
            {
                controller.enabled = controllerWasEnabled;
            }

            _railAnimating = false;
            _railRoutine = null;
        }

        private void ToggleCursor()
        {
            _cursorVisible = !_cursorVisible;
            Cursor.visible = _cursorVisible;
            Cursor.lockState = _cursorVisible ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void AdjustRailDuration(float delta)
        {
            _railDuration = Mathf.Max(1f, _railDuration + delta);
        }

        private void ToggleRailEasing()
        {
            _railUseSinEase = !_railUseSinEase;
        }

        // Steam debug data
        private bool _showSteamDebugWindow;
        private string _steamDebugInfo = "";
        private List<string> _steamDebugLobbies = new List<string>();
        private Vector2 _steamDebugScrollPos;

        private void DebugSteamLobbies()
        {
            _showSteamDebugWindow = !_showSteamDebugWindow;
            
            if (!_showSteamDebugWindow)
                return;

#if !DISABLESTEAMWORKS
            if (!SteamManager.Initialized)
            {
                _steamDebugInfo = "Steam NOT initialized!";
                _steamDebugLobbies.Clear();
                Debug.Log("[SteamDebug] Steam not initialized!");
                return;
            }

            var mySteamId = Steamworks.SteamUser.GetSteamID().m_SteamID;
            var myName = Steamworks.SteamFriends.GetPersonaName();
            var session = GameSystems.Steam.Scripts.SteamLobbySession.Instance;
            
            _steamDebugInfo = $"SteamID: {mySteamId}\n" +
                              $"Name: {myName}\n" +
                              $"In Lobby: {(session.IsInLobby ? session.CurrentLobbyId.ToString() : "NONE")}\n" +
                              $"Is Host: {session.IsHost}";

            Debug.Log("[SteamDebug] ========== STEAM LOBBY DEBUG ==========");
            Debug.Log($"[SteamDebug] Current SteamID: {mySteamId}");
            Debug.Log($"[SteamDebug] Persona Name: {myName}");
            Debug.Log($"[SteamDebug] Current Lobby: {(session.IsInLobby ? session.CurrentLobbyId.ToString() : "NONE")}");
            Debug.Log($"[SteamDebug] Is Host: {session.IsHost}");

            // Request ALL lobbies without any filter
            _steamDebugLobbies.Clear();
            _steamDebugLobbies.Add("Loading...");
            
            Debug.Log("[SteamDebug] Requesting ALL lobbies (no filter)...");
            Steamworks.SteamMatchmaking.AddRequestLobbyListResultCountFilter(100);
            var call = Steamworks.SteamMatchmaking.RequestLobbyList();
            _debugLobbyListCall ??= Steamworks.CallResult<Steamworks.LobbyMatchList_t>.Create(OnDebugLobbyMatchList);
            _debugLobbyListCall.Set(call);
#else
            _steamDebugInfo = "Steamworks is DISABLED!";
            _steamDebugLobbies.Clear();
            Debug.Log("[SteamDebug] Steamworks is disabled!");
#endif
        }

#if !DISABLESTEAMWORKS
        private Steamworks.CallResult<Steamworks.LobbyMatchList_t> _debugLobbyListCall;

        private void OnDebugLobbyMatchList(Steamworks.LobbyMatchList_t param, bool ioFailure)
        {
            _steamDebugLobbies.Clear();
            
            if (ioFailure)
            {
                _steamDebugLobbies.Add("ERROR: IO failure!");
                Debug.LogWarning("[SteamDebug] RequestLobbyList failed (IO failure)!");
                return;
            }

            int count = (int)param.m_nLobbiesMatching;
            Debug.Log($"[SteamDebug] ===== Found {count} total lobbies (unfiltered) =====");
            
            _steamDebugLobbies.Add($"=== Found {count} lobbies (NO FILTER) ===");
            _steamDebugLobbies.Add("");

            for (int i = 0; i < count; i++)
            {
                var lobbyId = Steamworks.SteamMatchmaking.GetLobbyByIndex(i);
                var ownerId = Steamworks.SteamMatchmaking.GetLobbyOwner(lobbyId);
                var hostName = Steamworks.SteamFriends.GetFriendPersonaName(ownerId);
                var memberCount = Steamworks.SteamMatchmaking.GetNumLobbyMembers(lobbyId);
                
                // Read lobby data
                string productTag = Steamworks.SteamMatchmaking.GetLobbyData(lobbyId, "rvsm_product") ?? "";
                string gameMode = Steamworks.SteamMatchmaking.GetLobbyData(lobbyId, "gamemode_asset") ?? "";
                
                bool isOurGame = productTag == "RabbitVsMole";
                string marker = isOurGame ? " [OURS]" : "";
                
                _steamDebugLobbies.Add($"[{i}]{marker} Host: {hostName}");
                _steamDebugLobbies.Add($"    LobbyID: {lobbyId.m_SteamID}");
                _steamDebugLobbies.Add($"    Members: {memberCount}");
                _steamDebugLobbies.Add($"    ProductTag: '{productTag}'");
                _steamDebugLobbies.Add($"    GameMode: '{gameMode}'");
                _steamDebugLobbies.Add("");
                
                Debug.Log($"[SteamDebug] Lobby[{i}]: ID={lobbyId.m_SteamID}, Host='{hostName}' ({ownerId.m_SteamID}), Members={memberCount}, ProductTag='{productTag}', GameMode='{gameMode}'");
            }
            
            if (count == 0)
            {
                _steamDebugLobbies.Add("No lobbies found!");
            }
            
            Debug.Log("[SteamDebug] ========================================");
        }
#endif

        private void DrawSteamDebugWindow()
        {
            if (!_showSteamDebugWindow)
                return;

            const float width = 500f;
            const float height = 400f;
            float x = Screen.width - width - 20f;
            float y = 20f;

            GUI.Box(new Rect(x, y, width, height), "Steam Lobby Debug (F11 to close)");
            
            // Info section
            GUI.Label(new Rect(x + 10f, y + 25f, width - 20f, 80f), _steamDebugInfo);
            
            // Lobby list with scroll
            Rect scrollViewRect = new Rect(x + 10f, y + 110f, width - 20f, height - 130f);
            Rect contentRect = new Rect(0, 0, width - 40f, _steamDebugLobbies.Count * 18f);
            
            _steamDebugScrollPos = GUI.BeginScrollView(scrollViewRect, _steamDebugScrollPos, contentRect);
            
            float yPos = 0f;
            foreach (var line in _steamDebugLobbies)
            {
                GUI.Label(new Rect(0, yPos, contentRect.width, 18f), line);
                yPos += 18f;
            }
            
            GUI.EndScrollView();
        }

        private void RandomizeFields()
        {
            var fields = Resources.FindObjectsOfTypeAll<FieldBase>();
            foreach (var field in fields)
            {
                if (field == null || !field.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (field is FarmFieldBase farm)
                {
                    farm.SetNewState(GetRandomFarmState(farm));
                    var max = GameManager.CurrentGameStats.FarmFieldMaxWaterLevel;
                    farm.SetWaterLevelFromNetwork(UnityEngine.Random.Range(0f, max));
                }
                else if (field is UndergroundFieldBase underground)
                {
                    underground.SetNewState(GetRandomUndergroundState(underground));
                }
            }
        }

        private FieldState GetRandomFarmState(FarmFieldBase farm)
        {
            var options = new Func<FieldState>[]
            {
                farm.CreateFarmCleanState,
                farm.CreateFarmMoundedState,
                farm.CreateFarmPlantedState,
                farm.CreateFarmRootedState,
                farm.CreateFarmWithCarrotState
            };

            return options[UnityEngine.Random.Range(0, options.Length)].Invoke();
        }

        private FieldState GetRandomUndergroundState(UndergroundFieldBase field)
        {
            var options = new Func<FieldState>[]
            {
                field.CreateUndergroundCleanState,
                field.CreateUndergroundMoundedState,
                field.CreateUndergroundWallState,
                field.CreateUndergroundCarrotState
            };

            return options[UnityEngine.Random.Range(0, options.Length)].Invoke();
        }

        private void ToggleOrbit()
        {
            if (_orbitActive)
            {
                StopOrbitIfRunning();
                return;
            }

            var cam = GetActiveCamera();
            if (cam == null)
            {
                return;
            }

            StartOrbit(cam);
        }

        private void StartOrbit(Camera cam)
        {
            StopOrbitIfRunning();

            _orbitPivot = cam.transform.position + cam.transform.forward * 3f;
            _orbitOffset = cam.transform.position - _orbitPivot;
            if (_orbitOffset.sqrMagnitude < 0.01f)
            {
                _orbitOffset = -cam.transform.forward * 3f;
            }

            var controller = cam.GetComponent<DebugFreeCamController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            _orbitRoutine = StartCoroutine(OrbitRoutine(cam, controller));
        }

        private IEnumerator OrbitRoutine(Camera cam, DebugFreeCamController controller)
        {
            _orbitActive = true;

            while (_orbitActive && cam != null)
            {
                var angle = _orbitSpeedDeg * Time.deltaTime;
                _orbitOffset = Quaternion.AngleAxis(angle, Vector3.up) * _orbitOffset;
                cam.transform.position = _orbitPivot + _orbitOffset;
                cam.transform.LookAt(_orbitPivot);
                yield return null;
            }

            if (controller != null)
            {
                controller.enabled = true;
            }

            _orbitRoutine = null;
            _orbitActive = false;
        }

        private void StopOrbitIfRunning()
        {
            if (_orbitRoutine != null)
            {
                StopCoroutine(_orbitRoutine);
                _orbitRoutine = null;
            }

            _orbitActive = false;
        }

        private void AdjustOrbitSpeed(float delta)
        {
            _orbitSpeedDeg = Mathf.Max(1f, _orbitSpeedDeg + delta);
        }

    }

    /// <summary>
    /// Simple mouse-look + arrow/WASD flight controller used by the debug FreeCam.
    /// </summary>
    public class DebugFreeCamController : MonoBehaviour
    {
        public float moveSpeed = 10f;
        public float lookSensitivity = 2f;

        private float _yaw;
        private float _pitch;

        private void Start()
        {
            var euler = transform.eulerAngles;
            _yaw = euler.y;
            _pitch = euler.x;
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var mouseDelta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
            _yaw += mouseDelta.x * lookSensitivity * Time.deltaTime;
            _pitch -= mouseDelta.y * lookSensitivity * Time.deltaTime;
#else
            _yaw += Input.GetAxis("Mouse X") * lookSensitivity;
            _pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
#endif
            _pitch = Mathf.Clamp(_pitch, -80f, 80f);

            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

#if ENABLE_INPUT_SYSTEM
            var moveInput = new Vector2(
                (IsPressed(Key.RightArrow) ? 1f : 0f) - (IsPressed(Key.LeftArrow) ? 1f : 0f),
                (IsPressed(Key.UpArrow) ? 1f : 0f) - (IsPressed(Key.DownArrow) ? 1f : 0f)
            );
#else
            var moveInput = new Vector2(
                (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f),
                (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.DownArrow) ? 1f : 0f)
            );
#endif

#if ENABLE_INPUT_SYSTEM
            var move = transform.right * moveInput.x + transform.forward * moveInput.y;
#else
            var move = transform.right * moveInput.x + transform.forward * moveInput.y;
#endif

            if (move.sqrMagnitude > 0f)
            {
                transform.position += move.normalized * moveSpeed * Time.deltaTime;
            }

#if ENABLE_INPUT_SYSTEM
            if (IsPressed(Key.Space))
            {
                transform.position += Vector3.up * moveSpeed * Time.deltaTime;
            }

            if (IsPressed(Key.LeftCtrl) || IsPressed(Key.RightCtrl))
            {
                transform.position += Vector3.down * moveSpeed * Time.deltaTime;
            }
#else
            if (Input.GetKey(KeyCode.Space))
            {
                transform.position += Vector3.up * moveSpeed * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.LeftControl))
            {
                transform.position += Vector3.down * moveSpeed * Time.deltaTime;
            }
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static bool IsPressed(Key key)
        {
            return Keyboard.current != null && Keyboard.current[key].isPressed;
        }
#endif
    }
}
#endif


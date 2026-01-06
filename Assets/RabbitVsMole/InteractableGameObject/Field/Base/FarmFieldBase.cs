using Extensions;
using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Base;
using RabbitVsMole.InteractableGameObject.Enums;
using RabbitVsMole.InteractableGameObject.Field;
using RabbitVsMole.InteractableGameObject.Visuals;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Mathematics.Geometry;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RabbitVsMole.InteractableGameObject.Field.Base
{
    public class FarmFieldBase : FieldBase
    {
        protected override FieldState _fieldState { get; set; }
        [Header("Water mesh")]
        [SerializeField] Mesh _meshWaterLevel0;
        [SerializeField] Mesh _meshWaterLevel1;
        [SerializeField] MeshFilter _modelMeshFilter;

        [Header("Visuals")]
        [SerializeField] FarmCarrotVisual farmCarrotVisualPrefab;
        FarmCarrotVisual _farmCarrotVisual;

        [SerializeField] RootsVisual rootsVisualPrefab;
        RootsVisual _rootsVisual;

        [SerializeField] MoundVisual moundVisualPrefab;
        MoundVisual _moundVisual;

        [SerializeField] SeedVisual seedVisualPrefab;
        SeedVisual _seedVisual;

        private float _waterLevelBacking;
        private bool _isMeshWet;
        private bool HaveWater;

        protected float WaterLevel
        {
            get => _waterLevelBacking;
            set
            {
                _waterLevelBacking = Mathf.Clamp(value, 0f, GameManager.CurrentGameStats.FarmFieldMaxWaterLevel);
                HaveWater = _waterLevelBacking > 0f;
                if (HaveWater != _isMeshWet)
                {
                    _isMeshWet = HaveWater;
                    RefreshWaterMesh(_isMeshWet);
                }
            }
        }

        public float GetWaterLevel() => WaterLevel;
        public float GetCarrotProgressNormalized() => _farmCarrotVisual != null ? _farmCarrotVisual.Progress : 0f;

        internal void SetWaterLevelFromNetwork(float value) => WaterLevel = value;
        internal void SetCarrotProgressFromNetwork(float progress)
        {
            if (_farmCarrotVisual == null)
                return;

            _farmCarrotVisual.SetProgressNormalized(progress);
        }

        private void RefreshWaterMesh(bool haveWater) =>
            _modelMeshFilter.mesh = haveWater 
                ? _meshWaterLevel1 
                : _meshWaterLevel0;

        public bool IsCarrotReady =>
            _farmCarrotVisual != null
            && _farmCarrotVisual.IsReady;

        public string StateName =>
            nameof(_fieldState);


        void Awake()
        {
            if (GameManager.CurrentGameStats.GameRulesFarmFieldStartsWithRoots)
                _fieldState = CreateFarmRootedState();
            else
                _fieldState = CreateFarmCleanState();

            if(_meshWaterLevel0 == null) { DebugHelper.LogError(this, "Property '_meshWaterLevel0' not set in inspector!"); return; }
            if(_meshWaterLevel1 == null){DebugHelper.LogError(this, "Property '_meshWaterLevel1' not set in inspector!");return;}
            if(_modelMeshFilter == null){DebugHelper.LogError(this, "Property '_modelMeshFilter' not set in inspector!");return;}
            if(farmCarrotVisualPrefab == null){DebugHelper.LogError(this, "Property 'farmCarrotVisualPrefab' not set in inspector!");return;}
            if(rootsVisualPrefab == null){DebugHelper.LogError(this, "Property 'rootsVisualPrefab' not set in inspector!");return;}
            if(moundVisualPrefab == null){DebugHelper.LogError(this, "Property 'moundVisualPrefab' not set in inspector!");return;}
            if(seedVisualPrefab == null){DebugHelper.LogError(this, "Property 'seedVisualPrefab' not set in inspector!");return;}
        }

        private Coroutine _growCarrotCoroutine;
        private IEnumerator HandleCarrotGrowth()
        {
            if (_farmCarrotVisual == null) 
                yield break;

            _farmCarrotVisual.SetDuration(GameManager.CurrentGameStats.CarrotGrowingTimeInSec);

            bool canGrow = GameManager.CurrentGameStats.SystemAllowToGrowCarrot && HaveWater;
            
            if (!canGrow)
                _farmCarrotVisual.Pause();
            
            SetCarrotGrowthAIPriority(canGrow);
            bool wasGrowing = canGrow;

            while (!IsCarrotReady)
            {
                canGrow = GameManager.CurrentGameStats.SystemAllowToGrowCarrot && HaveWater;

                // Reaguj na zmiany stanu
                if (canGrow != wasGrowing)
                {
                    if (canGrow)
                        _farmCarrotVisual.Resume();
                    else
                        _farmCarrotVisual.Pause();

                    SetCarrotGrowthAIPriority(canGrow);
                    wasGrowing = canGrow;
                }

                // Zużywaj wodę tylko gdy marchewka faktycznie rośnie
                if (canGrow)
                    WaterLevel -= GameManager.CurrentGameStats.FarmFieldWaterDrainByCarrotPerSec * Time.deltaTime;

                yield return null;
            }

            _farmCarrotVisual.SetDuration(0.5f);
            _growCarrotCoroutine = null;
            _fieldState.SetAIPriority(GameManager.CurrentGameStats.AIStats.FieldWithCarrotReady);
            _farmCarrotVisual.StartGlow();
            OnCarrotReady();
        }

        private void SetCarrotGrowthAIPriority(bool isGrowing) =>
            _fieldState.SetAIPriority(isGrowing
                ? GameManager.CurrentGameStats.AIStats.FarmFieldWithCarrotWorking
                : GameManager.CurrentGameStats.AIStats.FarmFieldWithCarrotNeedWater);

        public void StartStealCarrot() {
            if (!IsCarrotReady)
                return;

            if(_stealCarrotCorutine != null)
            {
                StopCoroutine(_stealCarrotCorutine);
            }
            _stealCarrotCorutine = StartCoroutine(AnimateCarrotPull(_farmCarrotVisual.transform, GameManager.CurrentGameStats.TimeActionStealCarrotFromUndergroundField, 1f));
        }
        public void StopStealCarrot()
        {
            if (_stealCarrotCorutine != null)
            {
                StopCoroutine(_stealCarrotCorutine);
                _stealCarrotCorutine = null;
            }
        }
        private Coroutine _stealCarrotCorutine;
        public IEnumerator AnimateCarrotPull(Transform carrotTransform, float duration, float pullDepth)
        {
            Vector3 startPos = carrotTransform.position;
            Quaternion startRot = carrotTransform.rotation;

            float elapsed = 0f;

            // Configuration for "chaotic but smooth" feel
            float shakeSpeed = 15f;    // How fast the chaotic vibration is
            float shakeAmount = 0.05f; // How strong the chaotic vibration is
            float wobbleSpeed = 4f;    // How fast the side-to-side tilt is
            float wobbleAngle = 8f;    // Max tilt angle in degrees

            while (elapsed < duration)
            {
                if (carrotTransform == null)
                {
                    StopStealCarrot();
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 1. Smoothly move downwards (Ease In/Out)
                float smoothStep = Mathf.SmoothStep(0, 1, t);
                Vector3 currentDepth = Vector3.down * (smoothStep * pullDepth);

                // 2. Add chaotic jitter using Perlin Noise
                // Perlin Noise gives smoother, "organic" randomness than Random.InsideUnitSphere
                float noiseX = Mathf.PerlinNoise(Time.time * shakeSpeed, 0) - 0.5f;
                float noiseZ = Mathf.PerlinNoise(0, Time.time * shakeSpeed) - 0.5f;
                Vector3 jitter = new Vector3(noiseX, 0, noiseZ) * shakeAmount;

                // 3. Add rhythmic wobble (side-to-side) using Sinus
                float wobble = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAngle;
                Quaternion tilt = Quaternion.Euler(wobble, 0, wobble * 0.5f);

                // Apply all transformations
                carrotTransform.position = startPos + currentDepth + jitter;
                carrotTransform.rotation = startRot * tilt;

                yield return null;
            }

            // Finalize: snap to target to avoid floating point errors
            carrotTransform.position = startPos + (Vector3.down * pullDepth);
        }

        private void Update()
        {
            WaterLevel -= GameManager.CurrentGameStats.FarmFieldWaterDrainPerSec;

            if (State is FarmFieldRooted && (!Online.OnlineAuthority.IsOnline || Online.OnlineAuthority.IsHost))
                HandleRoots();
        }
#if UNITY_EDITOR
        StringBuilder sb = new StringBuilder();
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;
            sb.Clear(); 
            sb.Append("Field State: ").AppendLine(State.GetType().Name);
            sb.AppendFormat("Water level: {0:0.00}/{1}\n", WaterLevel, GameManager.CurrentGameStats.FarmFieldMaxWaterLevel);
            if (_farmCarrotVisual != null)
            {
                sb.Append("Carrot progress: ")
                  .Append(Mathf.RoundToInt(_farmCarrotVisual.Progress * 100f))
                  .AppendLine("%");
            }

            if (State is FarmFieldRooted roots)
            {
                sb.Append("Roots hp: ").Append(roots.GetHP).AppendLine("%");
            }


            GUIStyle style = new GUIStyle();
            style.fontSize = 15;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;

            style.normal.background = Texture2D.whiteTexture;
            GUIContent content = new GUIContent(sb.ToString());

            Vector2 size = style.CalcSize(content);

            float padding = 10f;
            float width = size.x + padding;
            float height = size.y + padding;

            Vector3 worldPos = transform.position + Vector3.up * 2.0f;
            Vector2 guiPos = HandleUtility.WorldToGUIPoint(worldPos);

            Handles.BeginGUI();
            GUI.backgroundColor = new Color(0, 0, 0, 1f);

            Rect rect = new Rect(guiPos.x - width / 2, guiPos.y - height / 2, width, height);
            GUI.Box(rect, content, style);

            Handles.EndGUI();
        }
#endif
        float rootSpreadCounter = 0f;
        private void HandleRoots()
        {
            if(RandomUtils.Chance(.95f)) // little bit of randomness
                rootSpreadCounter += Time.deltaTime * GameManager.CurrentGameStats.RootsTickRate;

            if (rootSpreadCounter < 1f)
                return;

            rootSpreadCounter = 0f; //reset counter

            var neighborsFieldList = GetNeighborsFields();
            if (neighborsFieldList.Count == 0)
                return;

            var neighborsRootChance = GameManager.CurrentGameStats.RootsBirthChance + GameManager.CurrentGameStats.RootsSpreadIncreaseByNeibour * neighborsFieldList.Count;
            if (!RandomUtils.Chance(neighborsRootChance))
                return;

            foreach(var neighbor in neighborsFieldList)
            {
                if (RandomUtils.Chance(GameManager.CurrentGameStats.RootsBirthChance)
                && CanGrowRoots(neighbor))
                    neighbor.SetNewState(neighbor.CreateFarmRootedState());
            }
        }

        private bool CanGrowRoots(FarmFieldBase farmFieldBase)
        {
            var stats = GameManager.CurrentGameStats;

            return farmFieldBase.State switch
            {
                FarmFieldClean => stats.GameRulesRootsCanSpawnOnCleanField,
                FarmFieldPlanted => stats.GameRulesRootsCanSpawnOnPlantedField,
                FarmFieldWithCarrot => farmFieldBase.IsCarrotReady
                    ? stats.GameRulesRootsCanSpawnOnWithCarrotFullGrowField
                    : stats.GameRulesRootsCanSpawnOnWithCarrotField,
                FarmFieldMounded => stats.GameRulesRootsCanSpawnOnMoundedField,
                FarmFieldRooted => false,
                _ => false 
            };
        }

        private List<FarmFieldBase> GetNeighborsFields()
        {
            var neighborsList = new List<FarmFieldBase>();
            Vector2Int? myXy = FarmManager.GetFieldXY(this);

            int currentX = myXy.Value.x;
            int currentY = myXy.Value.y;
            int offset = GameManager.CurrentGameStats.RootsSpreadRadius;

            for (int xOffset = -offset; xOffset <= offset; xOffset++)
            {
                for (int yOffset = -offset; yOffset <= offset; yOffset++)
                {
                    if (xOffset == 0 && yOffset == 0) continue;

                    int targetX = currentX + xOffset;
                    int targetY = currentY + yOffset;

                    var field = FarmManager.GetFarmField(targetX, targetY);

                    if (field != null)
                        neighborsList.Add(field);
                }
            }
            return neighborsList;
        }

        internal void AddWater(float value)
        {
            WaterLevel += value;

            if (Online.OnlineAuthority.IsOnline && Online.OnlineAuthority.IsHost)
            {
                Online.OnlineAuthority.NotifyHostFieldStateChanged(this, _fieldState);
            }
        }

        internal void DestroyCarrot()
        {
            if (_growCarrotCoroutine != null)
                StopCoroutine(_growCarrotCoroutine);

            if (_farmCarrotVisual == null)
                return;

            _farmCarrotVisual.Hide();
            _farmCarrotVisual = null;
            OnCarrotDestroy();
        }

        void OnCarrotReady()
        {
            if(LinkedField != null && LinkedField.State is UndergroundFieldClean)
                LinkedField.SetNewState(LinkedField.CreateUndergroundCarrotState());
        }

        void OnCarrotDestroy()
        {
            if (LinkedField != null && LinkedField.State is UndergroundFieldCarrot)
                LinkedField.SetNewState(LinkedField.CreateUndergroundCleanState());
        }

        private void DestroyVisual<T>(ref T visual) where T : VisualBase
        {
            if (visual == null)
                return;
            visual.Hide();
            visual = null;
        }

        private void CreateVisual<T>(ref T visual, T prefab) where T : VisualBase
        {
            if (visual != null)
                return;
            visual = Instantiate(prefab, transform);
        }

        internal void DestroyRoots()
        {
            DestroyVisual(ref _rootsVisual);
        }

        internal void DestroyMound()
        {
            DestroyVisual(ref _moundVisual);
        }

        internal void DestroySeed()
        {
            DestroyVisual(ref _seedVisual);
        }

        internal void CreateMound()
        {
            CreateVisual(ref _moundVisual, moundVisualPrefab);
        }

        internal void CreateCarrot()
        {
            if (_farmCarrotVisual != null)
                return;
            CreateVisual(ref _farmCarrotVisual, farmCarrotVisualPrefab);
            _growCarrotCoroutine = StartCoroutine(HandleCarrotGrowth());
        }

        internal void CreateSeed()
        {
            CreateVisual(ref _seedVisual, seedVisualPrefab);
        }

        internal void CreateRoots()
        {
            CreateVisual(ref _rootsVisual, rootsVisualPrefab);
        }

        public void SetAIPriority(AIPriority priority)
        {
            _fieldState?.SetAIPriority(priority);
        }

        public override void LightUp(PlayerType playerType)
        {
            FarmFieldTileHighlighter.Instance(playerType)?.SetTarget(transform.position);
        }

        public override void LightDown(PlayerType playerType)
        {
            FarmFieldTileHighlighter.Instance(playerType)?.Hide();
        }

    }
}
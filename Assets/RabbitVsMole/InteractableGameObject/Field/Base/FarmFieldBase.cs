using Extensions;
using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Base;
using RabbitVsMole.InteractableGameObject.Enums;
using RabbitVsMole.InteractableGameObject.Field;
using RabbitVsMole.InteractableGameObject.Visuals;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
                _waterLevelBacking = Mathf.Clamp(value, 0f, GameInspector.GameStats.FarmFieldMaxWaterLevel);
                HaveWater = _waterLevelBacking > 0f;
                if (HaveWater != _isMeshWet)
                {
                    _isMeshWet = HaveWater;
                    RefreshWaterMesh(_isMeshWet);
                }
            }
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

            _farmCarrotVisual.SetDuration(GameInspector.GameStats.CarrotGrowingTimeInSec);
            
            if (HaveWater)
            {
                _farmCarrotVisual.Resume();
                _fieldState.SetAIPriority(GameInspector.GameStats.AIStats.FarmFieldWithCarrotWorking);
            }
            else
            {
                _farmCarrotVisual.Pause();
                _fieldState.SetAIPriority(GameInspector.GameStats.AIStats.FarmFieldWithCarrotNeedWater);
            }
            
            bool prevHaveWater = HaveWater;
            while (!IsCarrotReady)
            {
                WaterLevel -= GameInspector.GameStats.FarmFieldWaterDrainByCarrotPerSec * Time.deltaTime;
                
                bool currentHaveWater = HaveWater;
                if(prevHaveWater != currentHaveWater)
                {
                    if (currentHaveWater)
                    {
                        _farmCarrotVisual.Resume();
                        _fieldState.SetAIPriority(GameInspector.GameStats.AIStats.FarmFieldWithCarrotWorking);
                    }
                    else
                    {
                        _farmCarrotVisual.Pause();
                        _fieldState.SetAIPriority(GameInspector.GameStats.AIStats.FarmFieldWithCarrotNeedWater);

                    }
                }
                
                prevHaveWater = currentHaveWater;
                yield return null;
            }

            _farmCarrotVisual.SetDuration(0.5f);
            _growCarrotCoroutine = null;
            _fieldState.SetAIPriority(GameInspector.GameStats.AIStats.FieldWithCarrotReady);
            _farmCarrotVisual.StartGlow();
            OnCarrotReady();
        }

        public void StartStealCarrot() {
            if (!IsCarrotReady)
                return;

            if(_stealCarrotCorutine != null)
            {
                StopCoroutine(_stealCarrotCorutine);
            }
            _stealCarrotCorutine = StartCoroutine(AnimateCarrotPull(_farmCarrotVisual.transform, GameInspector.GameStats.TimeActionStealCarrotFromUndergroundField, 1f));
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
            WaterLevel -= GameInspector.GameStats.FarmFieldWaterDrainPerSec;

            if(State is FarmFieldRooted) 
                HandleRoots();
        }

        float rootSpreadCounter = 0f;
        private void HandleRoots()
        {
            if(RandomUtils.Chance(.95f)) // little bit of randomness
                rootSpreadCounter += Time.deltaTime * GameInspector.GameStats.RootsTickRate;

            if (rootSpreadCounter < 1f)
                return;

            rootSpreadCounter = 0f; //reset counter

            var neighborsFieldList = GetNeighborsFields();
            if (neighborsFieldList.Count == 0)
                return;

            var neighborsRootChance = GameInspector.GameStats.RootsBirthChance + GameInspector.GameStats.RootsSpreadIncreaseByNeibour * neighborsFieldList.Count;
            if (!RandomUtils.Chance(neighborsRootChance))
                return;

            foreach(var neighbor in neighborsFieldList)
            {
                if (RandomUtils.Chance(GameInspector.GameStats.RootsBirthChance)
                && CanGrowRoots(neighbor))
                    neighbor.SetNewState(neighbor.CreateFarmRootedState());
            }
        }

        private bool CanGrowRoots(FarmFieldBase farmFieldBase)
        {
            var stats = GameInspector.GameStats;

            return farmFieldBase.State switch
            {
                FarmFieldClean => stats.RootsCanSpawnOnCleanField,
                FarmFieldPlanted => stats.RootsCanSpawnOnPlantedField,
                FarmFieldWithCarrot => farmFieldBase.IsCarrotReady
                    ? stats.RootsCanSpawnOnWithCarrotFullGrowField
                    : stats.RootsCanSpawnOnWithCarrotField,
                FarmFieldMounded => stats.RootsCanSpawnOnMoundedField,
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
            int offset = GameInspector.GameStats.RootsSpreadRadius;

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

        internal void AddWater(float value) =>
            WaterLevel += value;

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
            if(LinkedField.State is UndergroundFieldClean)
                LinkedField.SetNewState(LinkedField.CreateUndergroundCarrotState());
        }

        void OnCarrotDestroy()
        {
            if (LinkedField.State is UndergroundFieldCarrot)
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
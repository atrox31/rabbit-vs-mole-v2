using Extensions;
using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Base;
using RabbitVsMole.InteractableGameObject.Enums;
using RabbitVsMole.InteractableGameObject.Field;
using RabbitVsMole.InteractableGameObject.Visuals;
using System;
using System.Collections;
using UnityEngine;

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
            _fieldState = CreateCleanState();
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
        }

        private void Update()
        {
            WaterLevel -= GameInspector.GameStats.FarmFieldWaterDrainPerSec;
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

        internal FieldState CreateCleanState() => new FarmFieldClean(this);
        internal FieldState CreatePlantedState() => new FarmFieldPlanted(this);
        internal FieldState CreateFarmMoundedState() => new FarmFieldMounded(this);
        internal FieldState CreateRootedState() => new FarmFieldRooted(this);
        internal FieldState CreateWithCarrotState() => new FarmFieldWithCarrot(this);

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
using System;
using System.Collections;
using Enums;
using GameObjects.Base;
using GameObjects.FarmField.States;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace GameObjects.FarmField
{
    public class FarmField : FieldBase, IInteractable
    {
        public IFarmFieldState State { get; private set; } = new UntouchedField();

        // Water - Data
        [Header("Water Settings")]
        [SerializeField] private float _maxWaterLevel = 2.0f;
        [SerializeField] private float _wateringRate = 0.5f;
        private float _currentWaterLevel;
        private bool _hasWaterModel;
        private Coroutine _wateringCoroutine;

        // UI and Particle Systems
        [Header("UI and Effects")]
        [SerializeField] private Image _waterFillIndicator; 
        [SerializeField] private ParticleSystem _particleSystem;

        // Water - Mesh
        private MeshFilter _modelMeshFilter;
        [SerializeField] private Mesh _dryMesh;
        [SerializeField] private Mesh _waterMesh;

        // Seed
        [Header("Seed Settings")]
        [SerializeField] private FarmSeed _seedPrefab;
        private FarmSeed _seed;

        // Roots
        [Header("Roots Settings")]
        [SerializeField] private GameObject _rootsPrefab;
        [SerializeField] private float _rootsChance = 0.333f;
        private GameObject _roots;

        public bool HasWater => _currentWaterLevel > 0;
        public bool IsWatering => _wateringCoroutine != null;
        public bool CanWaterField => _currentWaterLevel < _maxWaterLevel && !HaveRoots;
        public bool CanPlantSeed => !HasSeed && !HasCarrot && !HasMound && !HaveRoots;
        public bool HasSeed => _seed != null;
        public bool CanHarvestCarrot => HasCarrot && _carrotObject.IsReady;
        public bool HaveRoots => _roots != null;
        public bool CanPlantRoots => !HaveRoots;

        private void Awake()
        {
            if (_waterFillIndicator == null)
            {
                Debug.LogError("Water Fill Indicator is not assigned!", this);
            }

            _waterFillIndicator.fillAmount = 0.0f;
            _waterFillIndicator.gameObject.SetActive(false);
            _modelMeshFilter = GetComponentInChildren<MeshFilter>();
        }

        private void Update()
        {
            HandleSeedState();
            HandleCarrotGrowth();
        }

        public override void PrepareForMoundCreation()
        {
            DeleteSeed();
            DeleteCarrot();
        }

        public void Interact(
            PlayerType type,
            Func<ActionType, bool> setActionType,
            Action<bool> changeIsBusy)
        {
            if (State is PlantedField && !_seed.IsReady())
            {
                Debug.Log("Can't interact because seed is not ready");
                return;
            }

            if (State is GrownField && !CanHarvestCarrot)
            {
                Debug.Log("Can't interact because carrot is not grown");
                return;
            }

            changeIsBusy(true);

            StartCoroutine(State.Interact(
                this,
                type,
                setActionType,
                newState =>
                {
                    State = newState;
                    changeIsBusy(false);
                }));
        }

        public void StartWatering()
        {
            if (IsWatering) return;

            _waterFillIndicator.gameObject.SetActive(true);
            _wateringCoroutine = StartCoroutine(WateringProcess());
        }

        public void StopWatering()
        {
            if (!IsWatering) return;

            StopCoroutine(_wateringCoroutine);
            _waterFillIndicator.gameObject.SetActive(false);
            _wateringCoroutine = null;
            _particleSystem?.Stop(false);
        }

        public void ClearWater()
        {
            _currentWaterLevel = 0.0f;
            SetMesh(_dryMesh);
            _hasWaterModel = false;
            StopWatering();
        }

        public override bool CreateCarrot()
        {
            if (HasCarrot || !HasSeed) return false;

            _carrotObject = Instantiate(_carrotPrefab, transform.position, Quaternion.identity, transform);
            _carrotObject.SetPosition(_seed.GetSeedPosition()); // Sets carrot position to _seedPrefab position
            _haveCarrot = true;

            return true;
        }

        public bool PlantSeed()
        {
            if (!CanPlantSeed) return false;
            _seed = Instantiate(_seedPrefab, transform.position, Quaternion.identity, transform);
            return true;
        }

        public bool DeleteSeed()
        {
            if (!HasSeed) return false;

            Destroy(_seed.gameObject);
            _seed = null;

            return true;
        }

        public bool HarvestCarrot()
        {
            if (!CanHarvestCarrot) return false;
            return DeleteCarrot();
        }

        public bool PlantRoots()
        {
            if (HaveRoots) return false;
            _roots = Instantiate(_rootsPrefab, transform.position, Quaternion.identity);
            return true;
        }

        public bool TryToPlantRoots()
        {
            if (HaveRoots) return false;
            if (Random.value <= _rootsChance) return PlantRoots();

            return false;
        }

        /// <summary>
        /// Changes water level
        /// </summary>
        /// <param name="amountPerSecond">Amount of water to removal in one second</param>
        /// <returns>Return true if water is removed completely</returns>
        public bool DrainField(float amountPerSecond)
        {
            _currentWaterLevel -= amountPerSecond * Time.deltaTime;
            SetProperWaterModel();

            if (_currentWaterLevel <= 0.0f)
            {
                _currentWaterLevel = 0.0f;
                return true;
            }

            return false;
        }

        private IEnumerator WateringProcess()
        {
            _particleSystem?.Play(false);

            while (_currentWaterLevel < _maxWaterLevel)
            {
                _currentWaterLevel += _wateringRate * Time.deltaTime;
                SetProperWaterModel();
                UpdateWaterFillUI();

                yield return null;
            }

            StopWatering();
            _currentWaterLevel = _maxWaterLevel;
            _particleSystem?.Stop(false);
        }

        private void UpdateWaterFillUI()
        {
            float fillAmount = Mathf.Clamp01(_currentWaterLevel / _maxWaterLevel);
            _waterFillIndicator.fillAmount = fillAmount;
        }

        private void SetProperWaterModel()
        {
            bool newWaterState = _currentWaterLevel > 0;

            if (newWaterState != _hasWaterModel)
            {
                _hasWaterModel = newWaterState;
                SetMesh(_hasWaterModel ? _waterMesh : _dryMesh);
            }
        }

        private void SetMesh(Mesh targetMesh)
        {
            if (_modelMeshFilter != null) _modelMeshFilter.mesh = targetMesh;
        }

        private void HandleSeedState()
        {
            if (HasSeed && _seed.IsReady() && HasWaterToGrow())
            {
                if (!HasCarrot) CreateCarrot();
                else _seed.Shrink();
            }
        }

        private void HandleCarrotGrowth()
        {
            if (HasCarrot)
            {
                if (_carrotObject.IsReady) return;
                if (_carrotObject.Grow(this))
                {
                    // marhcewka gotowa
                }
            }
        }

        private bool HasWaterToGrow() => _currentWaterLevel > 0;
    }
}
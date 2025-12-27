using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Base;
using RabbitVsMole.InteractableGameObject.Enums;
using RabbitVsMole.InteractableGameObject.Field;
using RabbitVsMole.InteractableGameObject.Visuals;
using System;
using UnityEngine;

namespace RabbitVsMole.InteractableGameObject.Field.Base
{
    public class UndergroundFieldBase : FieldBase
    {
        protected override FieldState _fieldState { get; set; }
        [Header("Stats")]


        [Header("Visuals")]
        [SerializeField] UndergroundCarrotVisual undergroundCarrotVisualPrefab;
        UndergroundCarrotVisual _undergroundCarrotVisual;

        [SerializeField] WallVisual wallVisualPrefab;
        WallVisual _wallsVisual;

        [SerializeField] MoundVisual moundVisualPrefab;
        MoundVisual _moundVisual;

        public bool IsCarrotReady =>
            _undergroundCarrotVisual != null
            && _undergroundCarrotVisual.IsReady;


        void Awake()
        {
            _fieldState = CreateWallState();
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

        internal void DestroyMound()
        {
            DestroyVisual(ref _moundVisual);
        }

        internal void CreateMound()
        {
            CreateVisual(ref _moundVisual, moundVisualPrefab);
        }

        internal void DestroyCarrot()
        {
            if (_undergroundCarrotVisual == null)
                return;
            _undergroundCarrotVisual.Hide();
            _undergroundCarrotVisual = null;
        }

        internal void CreateCarrot()
        {
            if (_undergroundCarrotVisual != null)
                return;
            CreateVisual(ref _undergroundCarrotVisual, undergroundCarrotVisualPrefab);
        }

        internal void DestroyWall()
        {
            DestroyVisual(ref _wallsVisual);
        }

        internal void CreateWall()
        {
            CreateVisual(ref _wallsVisual, wallVisualPrefab);
        }

        public void SetAIPriority(AIPriority priority)
        {
            _fieldState?.SetAIPriority(priority);
        }

        internal FieldState CreateWallState() => new UndergroundFieldWall(this);
        internal FieldState CreateUndergroundMoundedState() => new UndergroundFieldMounded(this);
        internal FieldState CreateUndergroundCarrotState() => new UndergroundFieldCarrot(this);
        internal FieldState CreateUndergroundCleanState() => new UndergroundFieldClean(this);

        public override void LightUp(PlayerType playerType)
        {

        }

        public override void LightDown(PlayerType playerType)
        {

        }
    }
}
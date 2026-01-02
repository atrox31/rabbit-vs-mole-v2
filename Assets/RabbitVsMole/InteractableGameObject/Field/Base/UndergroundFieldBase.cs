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

        [Header("Visuals")]
        [SerializeField] UndergroundCarrotVisual undergroundCarrotVisualPrefab;
        UndergroundCarrotVisual _undergroundCarrotVisual;
        public bool IsCarrotReady =>
            _undergroundCarrotVisual != null
            && _undergroundCarrotVisual.IsReady;

        [SerializeField] WallVisual wallVisualPrefab;
        WallVisual _wallsVisual;
        public bool HaveWall =>
            _wallsVisual != null;

        [SerializeField] MoundVisual moundVisualPrefab;
        MoundVisual _moundVisual;



        void Awake()
        {
            _fieldState = CreateUndergroundWallState();
            _wallsVisual?.SetDuration(0.01f);

        }
        VisualBase _currentVisual;

        private void DestroyVisual<T>(ref T visual) where T : VisualBase
        {
            if (visual == null)
                return;
            visual.Hide();
            visual = null;
            _currentVisual = null;
        }

        private void CreateVisual<T>(ref T visual, T prefab) where T : VisualBase
        {
            if (visual != null)
                return;
            visual = Instantiate(prefab, transform);
            _currentVisual = visual;
        }

        internal void DestroyMound()
        {
            _moundVisual?.SetDuration(GameManager.CurrentGameStats.TimeActionCollapseMound);
            DestroyVisual(ref _moundVisual);
        }

        internal void CreateMound()
        {
            CreateVisual(ref _moundVisual, moundVisualPrefab);
        }

        internal void DestroyCarrot()
        {
            DestroyVisual(ref _undergroundCarrotVisual);
        }

        internal void CreateCarrot()
        {
            CreateVisual(ref _undergroundCarrotVisual, undergroundCarrotVisualPrefab);
        }

        internal void DestroyWall()
        {
            _wallsVisual?.SetDuration(GameManager.CurrentGameStats.TimeActionDigUndergroundWall);
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
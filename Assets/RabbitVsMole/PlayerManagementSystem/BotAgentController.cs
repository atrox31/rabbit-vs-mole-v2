using Extensions;
using GameObjects;
using PlayerManagementSystem;
using PlayerManagementSystem.AIBehaviour.Common;
using System.Linq;
using Unity.Behavior;
using Unity.VisualScripting;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using static RabbitVsMole.GameManager;

namespace RabbitVsMole
{

    public class BotAgentController : BotAgentControllerBase<PlayerType, PlayerAvatar>
    {
        private PlayerType _playerType;
        private BehaviorGraphAgent _graphAgent;
        private BlackboardReference _blackboardReference;
        private float _intelligence;

        private float _currentAggroRange;
        private float _aggroCounter = 0f;
        private bool _peace = false;
        private bool _chase = false;
        private PlayerAvatar _enemy;
        private float GetDefaultAggroRange => _intelligence.Map(
                AIConsts.MIN_INTELIGENCE, AIConsts.MAX_INTELIGENCE,
                AIConsts.MIN_AGGRO_SPHERE_RADIUS, AIConsts.MAX_AGGRO_SPHERE_RADIUS);
        private float GetAggroModyficator =>
            (AIConsts.AGGRO_INCREASE_RATIO_BY_INTELIIGENCE * _intelligence) * Time.deltaTime;
        public static void CreateInstance(PlayGameSettings playGameSettings, PlayerType playerType, int intelligence = 80)
        {
            var prefab = _agentPrefabs.GetPrefab(PlayerControlAgent.Bot);
            var instance = Instantiate(prefab).GetOrAddComponent<BotAgentController>();

            if (instance == null)
            {
                DebugHelper.LogError(null, "Failed to instantiate BotAgentController prefab");
                return;
            }

            instance._playerType = playerType;
            instance._intelligence = intelligence;
            instance._currentAggroRange = instance.GetDefaultAggroRange;
            if(playGameSettings.gameMode.winCondition == GameModeWinCondition.Cooperation)
                instance._peace = true;

            if (!instance.Initialize(playerType))
            {
                DebugHelper.LogError(instance, "Failed to initialize BotAgentController");
                Destroy(instance.gameObject);
                return;
            }

            if (!instance.SetupBehaviorGraphAgent(intelligence))
            {
                DebugHelper.LogError(instance, "Failed to initialize BehaviorGraphAgent");
                Destroy(instance.gameObject);
                return;
            }

        }

        private void Start()
        {
            if (!_peace)
            {
                SetupEnemy();
            }
        }

        private void Update()
        {
            if (!_peace)
            {
                if (_chase)
                    HandleChase();
                else
                    HandleAggroWatcher();
            }
        }

        private bool VectorDistanceMagnitude(Vector3 pos1, Vector3 pos2, float distance)
        {
            Vector2 diff = new Vector2(pos1.x - pos2.x, pos1.z - pos2.z);
            return diff.sqrMagnitude < (distance * distance);
        }

        public void CalmDown()
        {
            _chase = false;
            _aggroCounter *= AIConsts.AGGRO_CALM_DOWN_RATION;
        }

        private void HandleChase()
        {
            if (!VectorDistanceMagnitude(
                    _playerAvatar.transform.position,
                    _enemy.transform.position,
                    _currentAggroRange))
            {

                _aggroCounter -= GetAggroModyficator;
                if(_aggroCounter < AIConsts.AGGRO_MIN_TO_CHASE)
                {
                    _aggroCounter = AIConsts.AGGRO_ZERO;
                    _chase = false;
                    return;
                }
            }
            else
            {
                if (_aggroCounter < AIConsts.AGGRO_MAX)
                    _aggroCounter += GetAggroModyficator / 2f;
                else
                    _aggroCounter = AIConsts.AGGRO_MAX;
            }
        }

        private void HandleAggroWatcher()
        {
            if (_aggroCounter >= AIConsts.AGGRO_MAX)
            {
                _chase = true;
            }
            else
            {
                if (VectorDistanceMagnitude(
                    _playerAvatar.transform.position,
                    _enemy.transform.position,
                    _currentAggroRange))
                {

                    _aggroCounter += GetAggroModyficator;
                }
            }
            if (!_blackboardReference.SetVariableValue("Chase", _chase))
            {
                DebugHelper.LogError(this, "Cannot find Chase");
            }
        }

        private void SetupEnemy()
        {
            _enemy = FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None).Where(pa => pa.playerType != _playerAvatar.playerType).First();
            if (_enemy is null)
            {
                DebugHelper.LogError(this, "Cannot find enemy, switch to peace");
                _peace = true;
                return;
            }

            if (!_blackboardReference.SetVariableValue("Enemy", _enemy.gameObject))
            {
                DebugHelper.LogError(this, "Cannot find Enemy");
                return;
            }
        }

        private bool SetupBehaviorGraphAgent(int intelligence)
        {
            
            if (!TryGetComponent<BehaviorGraphAgent>(out _graphAgent))
            {
                DebugHelper.LogError(this, "Failed to find BehaviorGraphAgent");
                return false;
            }
            
            if(_playerType == PlayerType.Mole)
            {
                _graphAgent.enabled = false;
            }

            _blackboardReference = _graphAgent.BlackboardReference;

            if (!SetVarible<FarmSeedSource>(_blackboardReference) 
                || !SetVarible<FarmWaterSource>(_blackboardReference) 
                || !SetVarible<FarmStorage>(_blackboardReference))
                return false;

            if(!_blackboardReference.SetVariableValue("AvatarOfPlayer", _playerAvatar.gameObject))
            {
                DebugHelper.LogError(this, "Cannot find AvatarOfPlayer");
                return false;
            }

            if(!_blackboardReference.SetVariableValue("Intelligence", intelligence))
            {
                DebugHelper.LogError(this, "Cannot find Intelligence");
                return false;
            }

            return true;
        }

        private bool SetVarible<T>(BlackboardReference blackboard) where T : UnityEngine.MonoBehaviour
        {
            T obj = GameObject.FindFirstObjectByType<T>();
            if (obj is null)
            {
                DebugHelper.LogError(this, $"Can not find object on scene of type{typeof(T).Name}");
                return false;
            }
            return blackboard.SetVariableValue(typeof(T).Name, obj.gameObject);
        }
    }
}
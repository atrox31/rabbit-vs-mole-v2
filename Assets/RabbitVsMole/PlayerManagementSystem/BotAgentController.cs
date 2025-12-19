using GameObjects;
using PlayerManagementSystem;
using RabbitVsMole;
using System;
using Unity.Behavior;
using UnityEngine;
using static RabbitVsMole.GameManager;

namespace RabbitVsMole
{

    public class BotAgentController : BotAgentControllerBase<PlayerType, PlayerAvatar>
    {
        private PlayerType _playerType;
        private BehaviorGraphAgent _graphAgent;
        public static void CreateInstance(PlayGameSettings playGameSettings, PlayerType playerType)
        {
            var prefab = _agentPrefabs.GetPrefab(PlayerControlAgent.Bot);
            var instance = Instantiate(prefab).GetComponent<BotAgentController>();

            if (instance == null)
            {
                DebugHelper.LogError(null, "Failed to instantiate BotAgentController prefab");
                return;
            }

            instance._playerType = playerType;

            if (!instance.Initialize(playerType))
            {
                DebugHelper.LogError(instance, "Failed to initialize BotAgentController");
                Destroy(instance.gameObject);
                return;
            }

            if (!instance.SetupBehaviorGraphAgent())
            {
                DebugHelper.LogError(instance, "Failed to initialize BehaviorGraphAgent");
                Destroy(instance.gameObject);
                return;
            }

        }

        private bool SetupBehaviorGraphAgent()
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

            BlackboardReference blackboard = _graphAgent.BlackboardReference;

            if (!SetVarible<FarmSeedSource>(blackboard) 
                || !SetVarible<FarmWaterSource>(blackboard) 
                || !SetVarible<FarmStorage>(blackboard))
                return false;

            if(!blackboard.SetVariableValue("AvatarOfPlayer", _playerAvatar.gameObject))
            {
                DebugHelper.LogError(this, "Cannot find AvatarOfPlayer");
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
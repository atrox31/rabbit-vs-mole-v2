using System;

namespace PlayerManagementSystem
{

    public abstract class BotAgentControllerBase<TypeOfPlayer, TypeOfBotAvatar> : AgentController<TypeOfPlayer> 
        where TypeOfPlayer : Enum 
        where TypeOfBotAvatar : PlayerAvatarBase
    {
        protected TypeOfBotAvatar _playerAvatar;

        protected bool Initialize(TypeOfPlayer playerType)
        {
            _playerAvatar = CreateAvatar<TypeOfBotAvatar>(playerType);

            if (_playerAvatar == null)
            {
                DebugHelper.LogError(this, "PlayerAvatar component not found on avatar");
                return false;
            }

            // TODO inicialize systems

            return true;

        }

        private void OnEnable()
        {

        }

        private void OnDisable()
        {

        }

        private void OnDestroy()
        {

        }
    }
}
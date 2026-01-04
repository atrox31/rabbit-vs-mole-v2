namespace RabbitVsMole.Events
{
    public struct GameEndedEvent
    {
        public GameModeWinCondition WinCondition;
        public WinConditionEvaluator.WinResult WinResult;
        public bool RabbitIsLocal;
        public bool MoleIsLocal;
    }
}



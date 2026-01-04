using System;

namespace RabbitVsMole.Events
{
    public struct GoldenCarrotCollectedEvent
    {
        public DayOfWeek DayOfWeek;
        public PlayerType PlayerType;
    }
}



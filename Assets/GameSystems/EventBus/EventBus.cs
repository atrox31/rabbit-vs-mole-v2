using System;
using System.Collections.Generic;

namespace GameSystems
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _events = new();
        public static void Subscribe<T>(Action<T> listener)
        {
            Type type = typeof(T);
            if (_events.ContainsKey(type))
            {
                _events[type] = (Action<T>)_events[type] + listener;
            }
            else
            {
                _events[type] = listener;
            }
        }

        public static void Unsubscribe<T>(Action<T> listener)
        {
            Type type = typeof(T);
            if (_events.ContainsKey(type))
            {
                var currentDel = (Action<T>)_events[type] - listener;
                if (currentDel == null)
                    _events.Remove(type);
                else
                    _events[type] = currentDel;
            }
        }

        public static void Publish<T>(T eventData)
        {
            Type type = typeof(T);
            if (_events.ContainsKey(type))
            {
                ((Action<T>)_events[type])?.Invoke(eventData);
            }
        }
    }
}
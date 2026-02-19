#nullable enable
using System;
using System.Collections.Generic;

namespace GuildReceptionist.GameDesign.Domain
{
    public static class SimpleEventBus
    {
        private static readonly Dictionary<Type, Delegate> HandlersByType = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            if (handler is null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var eventType = typeof(T);
            if (HandlersByType.TryGetValue(eventType, out var existing))
            {
                HandlersByType[eventType] = Delegate.Combine(existing, handler);
                return;
            }

            HandlersByType[eventType] = handler;
        }

        public static void Publish<T>(T eventData)
        {
            var eventType = typeof(T);
            if (!HandlersByType.TryGetValue(eventType, out var del))
            {
                return;
            }

            if (del is Action<T> callback)
            {
                callback.Invoke(eventData);
            }
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (handler is null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var eventType = typeof(T);
            if (!HandlersByType.TryGetValue(eventType, out var existing))
            {
                return;
            }

            var updated = Delegate.Remove(existing, handler);
            if (updated is null)
            {
                HandlersByType.Remove(eventType);
                return;
            }

            HandlersByType[eventType] = updated;
        }
    }
}

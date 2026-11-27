using System;
using System.Collections.Generic;
using UnityEngine;

namespace Demo02.Core
{
    public interface IGameFlowState
    {
        bool IsPaused { get; }
        bool IsGameEnding { get; }
    }

    /// <summary>
    /// Composition-root entry point for runtime services. Features consume
    /// contracts from here instead of discovering concrete scene objects.
    /// </summary>
    public static class GameServices
    {
        private static readonly Dictionary<Type, object> Services = new();

        public static void Register<T>(T service)
            where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }
            Services[typeof(T)] = service;
        }

        public static bool TryGet<T>(out T service)
            where T : class
        {
            if (Services.TryGetValue(typeof(T), out object value)
                && value is T typed)
            {
                service = typed;
                return true;
            }
            service = null;
            return false;
        }

        public static T GetOrNull<T>()
            where T : class
        {
            return TryGet(out T service) ? service : null;
        }

        public static bool Unregister<T>(T expected)
            where T : class
        {
            if (expected == null)
            {
                return false;
            }
            if (!Services.TryGetValue(typeof(T), out object value)
                || !ReferenceEquals(value, expected))
            {
                return false;
            }
            return Services.Remove(typeof(T));
        }

        public static void Clear()
        {
            Services.Clear();
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            Clear();
        }
    }
}

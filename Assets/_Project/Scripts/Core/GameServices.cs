using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniMayhem
{
    /// <summary>
    /// Tiny service locator. The only "global" in the project: systems register themselves here
    /// at bootstrap and everything else looks them up instead of using singletons.
    /// </summary>
    public static class GameServices
    {
        static readonly Dictionary<Type, object> services = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => services.Clear();

        public static void Register<T>(T service) where T : class => services[typeof(T)] = service;

        public static void Unregister<T>(T service) where T : class
        {
            if (services.TryGetValue(typeof(T), out var existing) && ReferenceEquals(existing, service))
                services.Remove(typeof(T));
        }

        public static T Get<T>() where T : class
        {
            if (!services.TryGetValue(typeof(T), out var obj)) return null;
            // Respect Unity's fake-null for destroyed objects.
            if (obj is UnityEngine.Object uo && uo == null) return null;
            return (T)obj;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            service = Get<T>();
            return service != null;
        }
    }
}

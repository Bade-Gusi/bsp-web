using System;
using System.Collections.Generic;

namespace BeiShuiCS2.Services
{
    /// <summary>
    /// 轻量服务容器，替代完整 DI 容器
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();
        private static readonly object _lock = new();

        public static void Register<T>(T service) where T : class
        {
            lock (_lock)
            {
                _services[typeof(T)] = service ?? throw new ArgumentNullException(nameof(service));
            }
        }

        public static T Get<T>() where T : class
        {
            lock (_lock)
            {
                if (_services.TryGetValue(typeof(T), out var service))
                    return (T)service;
                throw new InvalidOperationException($"Service {typeof(T).Name} not registered");
            }
        }

        public static T? TryGet<T>() where T : class
        {
            lock (_lock)
            {
                if (_services.TryGetValue(typeof(T), out var service))
                    return (T)service;
                return null;
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _services.Clear();
            }
        }
    }
}

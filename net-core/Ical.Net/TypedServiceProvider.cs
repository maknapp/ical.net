using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ical.Net
{
    public sealed class TypedServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public object GetService(Type serviceType)
        {
            _services.TryGetValue(serviceType, out object service);
            return service;
        }

        public T GetService<T>()
        {
            var service = GetService(typeof(T));
            if (service is T)
            {
                return (T)service;
            }
            return default;
        }

        /// <summary>
        /// Stores a unique entry for the type and each interface of the supplied object. If multiple objects 
        /// implement the same interface only the last one added will be retrievable under the interface type.
        /// </summary>
        public void SetService(object obj)
        {
            if (obj == null) { return; }

            var type = obj.GetType();
            _services[type] = obj;

            // Get interfaces for the given type
            foreach (var iface in type.GetInterfaces())
            {
                _services[iface] = obj;
            }
        }

        public void RemoveService(Type type)
        {
            if (type != null)
            {
                if (_services.ContainsKey(type))
                {
                    _services.Remove(type);
                }

                // TODO: Validate that the wrong type cannot be removed by mistake if multiple classes are implementing the same interface.

                // Get interfaces for the given type
                foreach (var iface in type.GetInterfaces().Where(iface => _services.ContainsKey(iface)))
                {
                    _services.Remove(iface);
                }
            }
        }
    }
}

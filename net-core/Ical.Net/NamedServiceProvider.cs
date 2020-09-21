using System.Collections.Generic;

namespace Ical.Net
{
    public sealed class NamedServiceProvider
    {
        private readonly Dictionary<string, object> _services = new Dictionary<string, object>();

        public object GetService(string name)
        {
            _services.TryGetValue(name, out object service);
            return service;
        }

        public T GetService<T>(string name)
        {
            object service = GetService(name);
            if (service is T)
            {
                return (T)service;
            }

            return default;
        }

        public void SetService(string name, object obj)
        {
            if (!string.IsNullOrEmpty(name) && obj != null)
            {
                _services[name] = obj;
            }
        }

        public void RemoveService(string name)
        {
            if (_services.ContainsKey(name))
            {
                _services.Remove(name);
            }
        }
    }
}

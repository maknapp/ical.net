using System;

namespace Ical.Net
{
    public sealed class ServiceProvider : ITypedServiceProvider, INamedServiceProvider
    {
        private readonly NamedServiceProvider _namedServices;
        private readonly TypedServiceProvider _typedServices;

        public ServiceProvider()
        {
            _namedServices = new NamedServiceProvider();
            _typedServices = new TypedServiceProvider();
        }

        public object GetService(Type serviceType) 
            => _typedServices.GetService(serviceType);

        public object GetService(string name) 
            => _namedServices.GetService(name);

        public T GetService<T>() 
            => _typedServices.GetService<T>();

        public T GetService<T>(string name) 
            => _namedServices.GetService<T>(name);

        public void SetService(string name, object obj) 
            => _namedServices.SetService(name, obj);

        public void SetService(object obj) 
            => _typedServices.SetService(obj);

        public void RemoveService(Type type) 
            => _typedServices.RemoveService(type);

        public void RemoveService(string name) 
            => _namedServices.RemoveService(name);
    }
}

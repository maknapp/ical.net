using System;

namespace Ical.Net
{
    public interface IServiceProvider : ITypedServiceProvider, INamedServiceProvider
    {
    }

    public interface ITypedServiceProvider
    {
        object GetService(Type type);
        T GetService<T>();
        void SetService(object obj);
        void RemoveService(Type type);
    }

    public interface INamedServiceProvider
    {
        object GetService(string name);
        T GetService<T>(string name);
        void SetService(string name, object obj);
        void RemoveService(string name);
    }
}

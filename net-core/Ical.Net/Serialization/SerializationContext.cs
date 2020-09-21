using System;
using System.Collections.Generic;

namespace Ical.Net.Serialization
{
    public class SerializationContext
    {
        private static SerializationContext _default;

        /// <summary>
        /// Gets the Singleton instance of the SerializationContext class.
        /// </summary>
        public static SerializationContext Default
        {
            get
            {
                if (_default == null)
                {
                    _default = new SerializationContext();
                }

                // Create a new serialization context that doesn't contain any objects
                // (and is non-static).  That way, if any objects get pushed onto
                // the serialization stack when the Default serialization context is used,
                // and something goes wrong and the objects don't get popped off the stack,
                // we don't need to worry (as much) about a memory leak, because the
                // objects weren't pushed onto a stack referenced by a static variable.
                var ctx = new SerializationContext
                {
                    _typedServices = _default._typedServices,
                };
                return ctx;
            }
        }

        private readonly Stack<WeakReference> _stack = new Stack<WeakReference>();
        private TypedServiceProvider _typedServices = new TypedServiceProvider();


        public SerializationContext()
        {
            // Add some services by default
            SetService(new SerializerFactory());
            SetService(new CalendarComponentFactory());
            SetService(new DataTypeMapper());
            SetService(new EncodingStack());
            SetService(new EncodingProvider());
        }

        public void Push(object item)
        {
            if (item != null)
            {
                _stack.Push(new WeakReference(item));
            }
        }

        public object Pop()
        {
            if (_stack.Count > 0)
            {
                var r = _stack.Pop();
                if (r.IsAlive)
                {
                    return r.Target;
                }
            }
            return null;
        }

        public object Peek()
        {
            if (_stack.Count > 0)
            {
                var r = _stack.Peek();
                if (r.IsAlive)
                {
                    return r.Target;
                }
            }
            return null;
        }

        public object GetService(Type serviceType)
            => _typedServices.GetService(serviceType);

        public T GetService<T>()
            => _typedServices.GetService<T>();

        public void SetService(object obj)
            => _typedServices.SetService(obj);

        public void RemoveService(Type serviceType)
            => _typedServices.RemoveService(serviceType);
    }
}

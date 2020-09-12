using System;

namespace Ical.Net
{
    public class CalendarObjectBase : ICopyable, ILoadable
    {
        private bool _loaded;

        public CalendarObjectBase()
        {
            _loaded = true;
        }

        /// <summary>
        /// Copies values from the target object to the
        /// current object.
        /// </summary>
        public virtual void CopyFrom(ICopyable c) {}

        /// <summary>
        /// Creates a copy of the object.
        /// </summary>
        /// <returns>The copy of the object.</returns>
        public virtual T Copy<T>()
        {
            var type = GetType();
            var obj = Activator.CreateInstance(type) as ICopyable;

            // Duplicate our values
            if (obj is T)
            {
                obj.CopyFrom(this);
                return (T) obj;
            }
            return default(T);
        }

        public virtual bool IsLoaded => _loaded;

        public event EventHandler Loaded;

        public virtual void OnLoaded()
        {
            _loaded = true;
            Loaded?.Invoke(this, EventArgs.Empty);
        }
    }
}
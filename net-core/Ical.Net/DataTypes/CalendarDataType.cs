using System;
using Ical.Net.Proxies;

namespace Ical.Net.DataTypes
{
    /// <summary>
    /// An abstract class from which all iCalendar data types inherit.
    /// </summary>
    public class CalendarDataType : ICalendarDataType, IEncodableDataType
    {
        private readonly IParameterCollection _parameters;
        private readonly ParameterCollectionProxy _proxy;
        private readonly ServiceProvider _serviceProvider;

        private ICalendarObject _associatedObject;

        public CalendarDataType()
        {
            _parameters = new ParameterList();
            _proxy = new ParameterCollectionProxy(_parameters);
            _serviceProvider = new ServiceProvider();
        }
        
        public Type GetValueType()
        {
            // See RFC 5545 Section 3.2.20.
            if (_proxy == null || !_proxy.ContainsKey("VALUE")) return null;

            switch (_proxy.Get("VALUE"))
            {
                case "BINARY":
                    return typeof (byte[]);
                case "BOOLEAN":
                    return typeof (bool);
                case "CAL-ADDRESS":
                    return typeof (Uri);
                case "DATE":
                    return typeof (IDateTime);
                case "DATE-TIME":
                    return typeof (IDateTime);
                case "DURATION":
                    return typeof (TimeSpan);
                case "FLOAT":
                    return typeof (double);
                case "INTEGER":
                    return typeof (int);
                case "PERIOD":
                    return typeof (Period);
                case "RECUR":
                    return typeof (RecurrencePattern);
                case "TEXT":
                    return typeof (string);
                case "TIME":
                    // TODO: implement ISO.8601.2004
                    throw new NotImplementedException();
                case "URI":
                    return typeof (Uri);
                case "UTC-OFFSET":
                    return typeof (UtcOffset);
                default:
                    return null;
            }
        }

        public void SetValueType(string type)
        {
            _proxy?.Set("VALUE", type?.ToUpper());
        }
        public string Encoding
        {
            get => Parameters.Get("ENCODING");
            set => Parameters.Set("ENCODING", value);
        }

        public ICalendarObject AssociatedObject
        {
            get => _associatedObject;
            set
            {
                if (Equals(_associatedObject, value))
                {
                    return;
                }

                _associatedObject = value;
                if (_associatedObject != null)
                {
                    _proxy.SetParent(_associatedObject);
                    if (_associatedObject is IParameterContainer)
                    {
                        _proxy.SetProxiedObject(((IParameterContainer) _associatedObject).Parameters);
                    }
                }
                else
                {
                    _proxy.SetParent(null);
                    _proxy.SetProxiedObject(_parameters);
                }
            }
        }

        public Calendar Calendar => _associatedObject?.Calendar;

        public string Language
        {
            get => Parameters.Get("LANGUAGE");
            set => Parameters.Set("LANGUAGE", value);
        }

        /// <summary>
        /// Copies values from the target object to the
        /// current object.
        /// </summary>
        public virtual void CopyFrom(ICopyable obj)
        {
            var dt = obj as ICalendarDataType;
            if (dt == null)
            {
                return;
            }

            _associatedObject = dt.AssociatedObject;
            _proxy.SetParent(_associatedObject);
            _proxy.SetProxiedObject(dt.Parameters);
        }

        /// <summary>
        /// Creates a copy of the object.
        /// </summary>
        /// <returns>The copy of the object.</returns>
        public T Copy<T>()
        {
            var type = GetType();
            var obj = Activator.CreateInstance(type) as ICopyable;

            // Duplicate our values
            if (obj is T)
            {
                obj.CopyFrom(this);
                return (T) obj;
            }
            return default;
        }

        public IParameterCollection Parameters => _proxy;

        public object GetService(Type serviceType) => _serviceProvider.GetService(serviceType);

        public object GetService(string name) => _serviceProvider.GetService(name);

        public T GetService<T>() => _serviceProvider.GetService<T>();

        public T GetService<T>(string name) => _serviceProvider.GetService<T>(name);

        public void SetService(string name, object obj) => _serviceProvider.SetService(name, obj);

        public void SetService(object obj) => _serviceProvider.SetService(obj);

        public void RemoveService(Type type) => _serviceProvider.RemoveService(type);

        public void RemoveService(string name) => _serviceProvider.RemoveService(name);
    }
}

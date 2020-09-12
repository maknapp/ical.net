using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Ical.Net.Serialization
{
    public sealed class GenericListSerializer : SerializerBase
    {
        private readonly Type _innerType;

        public GenericListSerializer(Type objectType)
        {
            _innerType = objectType.GetGenericArguments()[0];

            var listDef = typeof (List<>);
            TargetType = listDef.MakeGenericType(typeof (object));
        }

        public override Type TargetType { get; }

        public override string SerializeToString(object obj) 
            => throw new NotImplementedException();

        private MethodInfo _addMethodInfo;
        public override object Deserialize(TextReader tr)
        {
            var property = SerializationContext.Peek() as ICalendarProperty;
            if (property == null)
            {
                return null;
            }

            // Get a serializer factory to deserialize the contents of this list
            var listObj = Activator.CreateInstance(TargetType);
            if (listObj == null)
            {
                return null;
            }

            // Get a serializer for the inner type
            var sf = GetService<ISerializerFactory>();
            var stringSerializer = sf.Build(_innerType, SerializationContext) as IStringSerializer;
            if (stringSerializer == null)
            {
                return null;
            }
            // Deserialize the inner object
            var value = tr.ReadToEnd();

            // If deserialization failed, pass the string value into the list.
            var objToAdd = stringSerializer.Deserialize(new StringReader(value)) ?? value;

            // FIXME: cache this
            if (_addMethodInfo == null)
            {
                _addMethodInfo = TargetType.GetMethod("Add");
            }

            // Determine if the returned object is an IList<ObjectType>, rather than just an ObjectType.
            var add = objToAdd as IList;
            if (add != null)
            {
                //Deserialization returned an IList<ObjectType>, instead of an ObjectType.  So enumerate through the items in the list and add
                //them individually to our list.
                foreach (var innerObj in add)
                {
                    _addMethodInfo.Invoke(listObj, new[] {innerObj});
                }
            }
            else
            {
                // Add the object to the list
                _addMethodInfo.Invoke(listObj, new[] {objToAdd});
            }
            return listObj;
        }
    }
}
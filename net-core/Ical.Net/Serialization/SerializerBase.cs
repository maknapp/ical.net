﻿using System;
using System.IO;
using System.Text;

namespace Ical.Net.Serialization
{
    public abstract class SerializerBase : IStringSerializer
    {
        protected SerializerBase(SerializationContext ctx)
        {
            SerializationContext = ctx ?? throw new ArgumentNullException(nameof(ctx));
        }

        protected SerializationContext SerializationContext { get; }

        public abstract Type TargetType { get; }

        public abstract string Serialize(object obj);
        public abstract object Deserialize(string value);
        
        public object Deserialize(Stream stream, Encoding encoding)
        {
            object obj;
            using (var sr = new StreamReader(stream, encoding))
            {
                var encodingStack = GetService<EncodingStack>();
                encodingStack.Push(encoding);
                obj = Deserialize(sr.ReadToEnd());
                encodingStack.Pop();
            }
            return obj;
        }

        public void Serialize(object obj, Stream stream, Encoding encoding)
        {
            // NOTE: we don't use a 'using' statement here because
            // we don't want the stream to be closed by this serialization.
            // Fixes bug #3177278 - Serialize closes stream

            const int defaultBufferSize = 1024;     //This is StreamWriter's built-in default buffer size
            using (var sw = new StreamWriter(stream, encoding, defaultBufferSize, leaveOpen: true))
            {
                // Push the current object onto the serialization stack
                SerializationContext.Push(obj);

                // Push the current encoding on the stack
                var encodingStack = GetService<EncodingStack>();
                encodingStack.Push(encoding);

                sw.Write(Serialize(obj));

                // Pop the current encoding off the serialization stack
                encodingStack.Pop();

                // Pop the current object off the serialization stack
                SerializationContext.Pop();
            }
        }

        protected T GetService<T>()
        {
            return SerializationContext != null ? SerializationContext.GetService<T>() : default;
        }
    }
}

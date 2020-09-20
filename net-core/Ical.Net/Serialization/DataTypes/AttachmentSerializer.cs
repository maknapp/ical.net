﻿using System;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization.DataTypes
{
    public sealed class AttachmentSerializer : DataTypeSerializer
    {
        public AttachmentSerializer() : base(SerializationContext.Default) { }

        public AttachmentSerializer(SerializationContext ctx) : base(ctx) { }

        public override Type TargetType => typeof (Attachment);

        public override string Serialize(object obj)
        {
            var a = obj as Attachment;
            if (a == null)
            {
                return null;
            }

            if (a.Uri != null)
            {
                if (a.Parameters.ContainsKey("VALUE"))
                {
                    // Ensure no VALUE type is provided
                    a.Parameters.Remove("VALUE");
                }

                return Encode(a, a.Uri.OriginalString);
            }
            if (a.Data == null)
            {
                return null;
            }

            // Ensure the VALUE type is set to BINARY
            a.SetValueType("BINARY");

            // BASE64 encoding for BINARY inline attachments.
            a.Parameters.Set("ENCODING", "BASE64");

            return Encode(a, a.Data);
        }

        public override object Deserialize(string value)
        {
            try
            {
                var a = CreateAndAssociate() as Attachment;
                // Decode the value, if necessary
                var data = DecodeData(a, value);

                // Get the currently-used encoding off the encoding stack.
                var encodingStack = SerializationContext.GetService<EncodingStack>();
                a.ValueEncoding = encodingStack.Current;

                // Get the format of the attachment
                var valueType = a.GetValueType();
                if (valueType == typeof(byte[]))
                {
                    // If the VALUE type is specifically set to BINARY,
                    // then set the Data property instead.                    
                    return new Attachment(data)
                    {
                        ValueEncoding = a.ValueEncoding,
                        AssociatedObject = a.AssociatedObject,
                    };
                }

                // The default VALUE type for attachments is URI.  So, let's
                // grab the URI by default.
                var uriValue = Decode(a, value);
                a.Uri = new Uri(uriValue);

                return a;
            }
            catch
            {
                // TODO: Ignore exceptions selectively
            }

            return null;
        }
    }
}

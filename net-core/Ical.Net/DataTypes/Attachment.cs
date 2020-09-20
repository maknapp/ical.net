using System;
using System.Linq;
using System.Text;
using Ical.Net.Serialization.DataTypes;
using Ical.Net.Utilities;

namespace Ical.Net.DataTypes
{
    /// <summary>
    /// Attachments represent the ATTACH element that can be associated with Alarms, Journals, Todos, and Events. There are two kinds of attachments:
    /// 1) A string representing a URI which is typically human-readable, OR
    /// 2) A base64-encoded string that can represent anything
    /// </summary>
    public sealed class Attachment : CalendarDataType
    {
        public Uri Uri { get; internal set; }
        public byte[] Data { get; }

        private Encoding _valueEncoding = System.Text.Encoding.UTF8;
        public Encoding ValueEncoding
        {
            get => _valueEncoding;
            set
            {
                if (value == null)
                {
                    return;
                }
                _valueEncoding = value;
            }
        }

        public string FormatType
        {
            get => Parameters.Get("FMTTYPE");
            set => Parameters.Set("FMTTYPE", value);
        }

        public Attachment() { }

        public Attachment(byte[] value)
        {
            if (value != null)
            {
                Data = value;
            }
        }

        public string GetDataUnencoded()
        {
            return Data == null ? string.Empty : ValueEncoding.GetString(Data);
        }

        // TODO: See if CopyFrom() method can be deleted.
        public override void CopyFrom(ICopyable obj) { }

        public bool Equals(Attachment other)
        {
            var firstPart = Equals(Uri, other.Uri) && ValueEncoding.Equals(other.ValueEncoding);
            return Data == null
                ? firstPart
                : firstPart && Data.SequenceEqual(other.Data);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Attachment) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Uri?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (CollectionHelpers.GetHashCode(Data));
                hashCode = (hashCode * 397) ^ (ValueEncoding?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }

    public static class AttachmentConvert
    {
        public static Attachment DeserializeObject(string attachmentValue)
        {
            if (string.IsNullOrWhiteSpace(attachmentValue))
            {
                throw new ArgumentException($"'{nameof(attachmentValue)}' cannot be null, empty or whitespace.", nameof(attachmentValue));
            }

            return new AttachmentSerializer().Deserialize(attachmentValue) as Attachment;
        }
    }
}

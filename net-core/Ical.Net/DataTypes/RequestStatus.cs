using System.IO;
using Ical.Net.Serialization.DataTypes;

namespace Ical.Net.DataTypes
{
    /// <summary>
    /// A class that represents the return status of an iCalendar request.
    /// </summary>
    public sealed class RequestStatus : EncodableDataType
    {
        public string Description { get; set; }

        public string ExtraData { get; set; }

        public StatusCode StatusCode { get; set; }
        
        public override void CopyFrom(ICopyable obj)
        {
            base.CopyFrom(obj);

            var rs = obj as RequestStatus;
            if (rs == null)
            {
                return;
            }

            if (rs.StatusCode != null)
            {
                StatusCode = rs.StatusCode;
            }
            Description = rs.Description;
            rs.ExtraData = rs.ExtraData;
        }

        public override string ToString()
        {
            var serializer = new RequestStatusSerializer();
            return serializer.SerializeToString(this);
        }

        public bool Equals(RequestStatus other)
        {
            return string.Equals(Description, other.Description) && string.Equals(ExtraData, other.ExtraData) &&
                   Equals(StatusCode, other.StatusCode);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((RequestStatus) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Description?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (ExtraData?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (StatusCode?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }
}

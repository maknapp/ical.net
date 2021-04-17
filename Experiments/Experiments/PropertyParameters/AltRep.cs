﻿using System;
using Experiments.Utilities;

namespace Experiments.PropertyParameters
{
    /// <summary>
    /// This parameter specifies a URI that points to an alternate representation for a textual property value.
    ///
    /// This parameter specifies a URI that points to an alternate representation for a textual property value.  A property specifying this
    /// parameter MUST also include a value that reflects the default representation of the text value.  The URI parameter value MUST be specified in a
    /// quoted-string.
    ///
    /// Note: While there is no restriction imposed on the URI schemes allowed for this parameter, Content Identifier (CID) [RFC2392], HTTP [RFC2616],
    /// and HTTPS [RFC2818] are the URI schemes most commonly used by current implementations.
    /// https://tools.ietf.org/html/rfc5545#section-3.2.1
    /// </summary>
    /// <example>
    /// DESCRIPTION;ALTREP="CID:part3.msg.970415T083000@example.com":
    /// Project XYZ Review Meeting will include the following agenda
    /// items: (a) Market Overview\, (b) Finances\, (c) Project Management
    /// </example>
    public readonly struct AltRep :
        IValueType
    {
        public string Name => "ALTREP";
        public string Value => Uri?.OriginalString;
        public Uri Uri { get; }
        public bool IsEmpty => Uri == null;

        public AltRep(Uri value)
        {
            Uri = value;
        }

        public AltRep(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                Uri = null;
            }
            
            Uri = new Uri(uri, UriKind.RelativeOrAbsolute);
        }

        /// <summary>
        /// ALTREP="CID:part3.msg.970415T083000@example.com"
        /// </summary>
        /// <returns></returns>
        public override string ToString() => this.NameEqualsQuotedValue();

        public static void VerifyAltRep(AltRep altRep, string parameter, string parameterName)
        {
            if (altRep.IsEmpty)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(parameter))
            {
                throw new ArgumentException("Alternative representations MUST include a default string parameter", parameterName);
            }
        }
    }
}
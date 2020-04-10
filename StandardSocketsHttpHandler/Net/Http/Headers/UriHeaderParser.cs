// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Net.Http.Headers
{
    // Don't derive from BaseHeaderParser since parsing is delegated to Uri.TryCreate() 
    // which will remove leading and trailing whitespace.
    internal class UriHeaderParser : HttpHeaderParser
    {
        private UriKind _uriKind;

        internal static readonly UriHeaderParser RelativeOrAbsoluteUriParser =
            new UriHeaderParser(UriKind.RelativeOrAbsolute);

        private UriHeaderParser(UriKind uriKind)
            : base(false)
        {
            _uriKind = uriKind;
        }

        public override string ToString(object value)
        {
            Debug.Assert(value is Uri);
            Uri uri = (Uri)value;

            if (uri.IsAbsoluteUri)
            {
                return uri.AbsoluteUri;
            }
            else
            {
                return uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
            }
        }
    }
}

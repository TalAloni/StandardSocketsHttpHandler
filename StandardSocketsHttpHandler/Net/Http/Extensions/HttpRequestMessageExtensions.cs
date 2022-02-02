using System;
using System.Net.Http.Headers;
using System.Reflection;

namespace System.Net.Http
{
    internal static class HttpRequestMessageExtensions
    {
        // Depending on runtime the backing field for headers is either _headers or headers:
        // _headers in .net core and some .net fx and mono versions (.NET Framework 4.6.1, Mono on Mac)
        // headers in other .net fx / mono versions.
        static FieldInfo _headersField = typeof(HttpRequestMessage).GetField("_headers", BindingFlags.Instance | BindingFlags.NonPublic) ??
            typeof(HttpRequestMessage).GetField("headers", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool HasHeaders(this HttpRequestMessage request)
        {
            HttpRequestHeaders headers = (HttpRequestHeaders)_headersField.GetValue(request);
            return headers != null;
        }
    }
}

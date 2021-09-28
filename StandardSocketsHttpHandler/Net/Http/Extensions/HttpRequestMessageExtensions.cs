using System;
using System.Net.Http.Headers;
using System.Reflection;

namespace System.Net.Http
{
    internal static class HttpRequestMessageExtensions
    {
        public static bool HasHeaders(this HttpRequestMessage request)
        {
#if NET461
            string headersFieldName = "headers";
#else
            // Note: The field name is _headers in .NET core 
            string headersFieldName = RuntimeUtils.IsDotNetFramework() ? "headers" : "_headers";
#endif            
            FieldInfo headersField = typeof(HttpRequestMessage).GetField(headersFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            HttpRequestHeaders headers = (HttpRequestHeaders)headersField.GetValue(request);
            return headers != null;
        }
    }
}

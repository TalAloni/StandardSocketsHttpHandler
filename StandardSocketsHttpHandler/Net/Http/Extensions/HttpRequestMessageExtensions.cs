using System;
using System.Net.Http.Headers;
using System.Reflection;

namespace System.Net.Http
{
    internal static class HttpRequestMessageExtensions
    {
        public static bool HasHeaders(this HttpRequestMessage request)
        {
            // Note: The field name is _headers in .NET core 
            bool isDotNetFramework = RuntimeUtils.IsDotNetFramework();
            string headersFieldName = isDotNetFramework ? "headers" : "_headers";
            FieldInfo headersField = typeof(HttpRequestMessage).GetField(headersFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (headersField == null && isDotNetFramework)
            {
                // Fallback for .NET Framework 4.6.1
                headersFieldName = "_headers";
                headersField = typeof(HttpRequestMessage).GetField(headersFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            }
            else if (headersField == null && RuntimeUtils.IsDotNetNative())
            {
                // In .NET Native reflection is not supported, GetField will always return null
                // If HttpRequestMesssage.Headers will be accessed when equal to null, HttpRequestHeaders will be instantiated.
                return true;
            }
            HttpRequestHeaders headers = (HttpRequestHeaders)headersField.GetValue(request);
            return headers != null;
        }
    }
}

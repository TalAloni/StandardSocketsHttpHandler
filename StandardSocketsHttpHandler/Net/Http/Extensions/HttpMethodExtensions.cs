using System;
using System.Diagnostics;

namespace System.Net.Http
{
    internal static class HttpMethodExtensions
    {
        public static bool MustHaveRequestBody(this HttpMethod method)
        {
            // Normalize before calling this
            Debug.Assert(ReferenceEquals(method, HttpMethodUtils.Normalize(method)));

            return !ReferenceEquals(method, HttpMethod.Get) && !ReferenceEquals(method, HttpMethod.Head) && !ReferenceEquals(method, HttpMethodUtils.Connect) &&
                   !ReferenceEquals(method, HttpMethod.Options) && !ReferenceEquals(method, HttpMethod.Delete);
        }
    }
}

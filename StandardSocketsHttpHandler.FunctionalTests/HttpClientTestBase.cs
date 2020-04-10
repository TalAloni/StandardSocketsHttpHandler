// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Security.Authentication;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientTestBase
    {
        // SP_PROT_TLS1_3 / SslProtocols.Tls13 in netcoreapp3.0
        protected const SslProtocols Tls13Protocol = (SslProtocols)12288;

        protected virtual bool UseSocketsHttpHandler => true;

        protected bool IsWinHttpHandler => !UseSocketsHttpHandler && PlatformDetection.IsWindows && !PlatformDetection.IsUap && !PlatformDetection.IsFullFramework;
        protected bool IsCurlHandler => !UseSocketsHttpHandler && !PlatformDetection.IsWindows;
        protected bool IsNetfxHandler => PlatformDetection.IsWindows && PlatformDetection.IsFullFramework;
        protected bool IsUapHandler => PlatformDetection.IsWindows && PlatformDetection.IsUap;

        protected HttpClient CreateHttpClient() => new HttpClient(CreateSocketsHttpHandler());

        protected static StandardSocketsHttpHandler CreateSocketsHttpHandler()
        {
            StandardSocketsHttpHandler handler = new StandardSocketsHttpHandler();
            return handler;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Test.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.PlatformAbstractions;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract class HttpClientHandlerTestBase
    {
        public readonly ITestOutputHelper _output;

        protected virtual bool UseSocketsHttpHandler => true;
        protected virtual bool UseHttp2 => false;

        protected bool IsWinHttpHandler => !UseSocketsHttpHandler && PlatformDetection.IsWindows && !PlatformDetection.IsUap;
        protected bool IsCurlHandler => !UseSocketsHttpHandler && !PlatformDetection.IsWindows;
        protected bool IsNetfxHandler => false;
        protected bool IsUapHandler => PlatformDetection.IsWindows && PlatformDetection.IsUap;

        public HttpClientHandlerTestBase(ITestOutputHelper output)
        {
            _output = output;
        }

        protected Version VersionFromUseHttp2 => GetVersion(UseHttp2);

        protected static Version GetVersion(bool http2) => http2 ? new Version(2, 0) : HttpVersion.Version11;

        protected virtual HttpClient CreateHttpClient() => CreateHttpClient(CreateSocketsHttpHandler());

        protected HttpClient CreateHttpClient(StandardHttpMessageHandler handler)
        {
#if NETFRAMEWORK
            if (UseHttp2)
            {
                handler = new Http2MessageAdapter(handler);
            }
#endif
            var client = new HttpClient(handler);
#if !NETFRAMEWORK
            SetDefaultRequestVersion(client, VersionFromUseHttp2);
#endif
            return client;
        }

        protected static HttpClient CreateHttpClient(string useHttp2String) =>
            CreateHttpClient(bool.Parse(useHttp2String));

        protected static HttpClient CreateHttpClient(bool useHttp2) =>
            CreateHttpClient(CreateSocketsHttpHandler(useHttp2), useHttp2);

        protected static HttpClient CreateHttpClient(StandardHttpMessageHandler handler, string useHttp2String) =>
            CreateHttpClient(handler, bool.Parse(useHttp2String));

        protected static HttpClient CreateHttpClient(StandardHttpMessageHandler handler, bool useHttp2)
        {
#if NETFRAMEWORK
            if (useHttp2)
            {
                handler = new Http2MessageAdapter(handler);
            }

            var client = new HttpClient(handler);
#else
            var client = new HttpClient(handler);
            SetDefaultRequestVersion(client, GetVersion(useHttp2));
#endif
            return client;
        }

        protected StandardSocketsHttpHandler CreateSocketsHttpHandler() => CreateSocketsHttpHandler(UseHttp2);

        protected static StandardSocketsHttpHandler CreateSocketsHttpHandler(string useHttp2LoopbackServerString) =>
            CreateSocketsHttpHandler(bool.Parse(useHttp2LoopbackServerString));

        protected static void SetDefaultRequestVersion(HttpClient client, Version version)
        {
            PropertyInfo pi = client.GetType().GetProperty("DefaultRequestVersion", BindingFlags.Public | BindingFlags.Instance);
            Debug.Assert(pi != null || !PlatformDetection.IsNetCore);
            pi?.SetValue(client, version);
        }

        protected static StandardSocketsHttpHandler CreateSocketsHttpHandler(bool useHttp2LoopbackServer = false)
        {
            StandardSocketsHttpHandler handler = new StandardSocketsHttpHandler();

            if (useHttp2LoopbackServer)
            {
                TestHelper.EnableUnencryptedHttp2IfNecessary(handler);
                handler.SslOptions.RemoteCertificateValidationCallback = SecurityHelper.AllowAllCertificates;
            }

            return handler;
        }

        protected LoopbackServerFactory LoopbackServerFactory =>
            UseHttp2 ?
                (LoopbackServerFactory)Http2LoopbackServerFactory.Singleton :
                Http11LoopbackServerFactory.Singleton;

        // For use by remote server tests

        public static readonly IEnumerable<object[]> RemoteServersMemberData = Configuration.Http.RemoteServersMemberData;

        protected HttpClient CreateHttpClientForRemoteServer(Configuration.Http.RemoteServer remoteServer)
        {
            return CreateHttpClientForRemoteServer(remoteServer, CreateSocketsHttpHandler());
        }

        protected HttpClient CreateHttpClientForRemoteServer(Configuration.Http.RemoteServer remoteServer, StandardSocketsHttpHandler httpClientHandler)
        {
            HttpMessageHandler wrappedHandler = httpClientHandler;

            wrappedHandler = new VersionCheckerHttpHandler(httpClientHandler, remoteServer.HttpVersion);

            var client = new HttpClient(wrappedHandler);
            SetDefaultRequestVersion(client, remoteServer.HttpVersion);
            return client;
        }

        private sealed class VersionCheckerHttpHandler : DelegatingHandler
        {
            private readonly Version _expectedVersion;

            public VersionCheckerHttpHandler(HttpMessageHandler innerHandler, Version expectedVersion)
                : base(innerHandler)
            {
                _expectedVersion = expectedVersion;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.Version != _expectedVersion)
                {
                    throw new Exception($"Unexpected request version: expected {_expectedVersion}, saw {request.Version}");
                }

                HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

                if (response.Version != _expectedVersion)
                {
                    throw new Exception($"Unexpected response version: expected {_expectedVersion}, saw {response.Version}");
                }

                return response;
            }
        }

#if NETFRAMEWORK
        private class Http2MessageAdapter : StandardHttpMessageHandler
        {
            private StandardHttpMessageHandler m_handler;

            public Http2MessageAdapter(StandardHttpMessageHandler handler)
            {
                m_handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.Version == new Version(1, 1))
                {
                    request.Version = new Version(2, 0);
                }

                return m_handler.SendRequestAsync(request, cancellationToken);
            }
        }
#endif
    }
}

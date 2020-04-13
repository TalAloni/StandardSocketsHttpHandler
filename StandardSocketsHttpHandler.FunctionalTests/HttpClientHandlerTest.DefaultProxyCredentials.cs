// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract class HttpClientHandler_DefaultProxyCredentials_Test : HttpClientHandlerTestBase
    {
        public HttpClientHandler_DefaultProxyCredentials_Test(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Default_Get_Null()
        {
            using (StandardSocketsHttpHandler handler = CreateSocketsHttpHandler())
            {
                Assert.Null(handler.DefaultProxyCredentials);
            }
        }

        [Fact]
        public void SetGet_Roundtrips()
        {
            using (StandardSocketsHttpHandler handler = CreateSocketsHttpHandler())
            {
                var creds = new NetworkCredential("username", "password", "domain");

                handler.DefaultProxyCredentials = null;
                Assert.Null(handler.DefaultProxyCredentials);

                handler.DefaultProxyCredentials = creds;
                Assert.Same(creds, handler.DefaultProxyCredentials);

                handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                Assert.Same(CredentialCache.DefaultCredentials, handler.DefaultProxyCredentials);
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UAP HTTP stack doesn't support .Proxy property")]
        [Fact]
        public async Task ProxyExplicitlyProvided_DefaultCredentials_Ignored()
        {
            var explicitProxyCreds = new NetworkCredential("rightusername", "rightpassword");
            var defaultSystemProxyCreds = new NetworkCredential("wrongusername", "wrongpassword");
            string expectCreds = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{explicitProxyCreds.UserName}:{explicitProxyCreds.Password}"));

            await LoopbackServer.CreateClientAndServerAsync(async proxyUrl =>
            {
                using (StandardSocketsHttpHandler handler = CreateSocketsHttpHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    handler.Proxy = new UseSpecifiedUriWebProxy(proxyUrl, explicitProxyCreds);
                    handler.DefaultProxyCredentials = defaultSystemProxyCreds;
                    using (HttpResponseMessage response = await client.GetAsync("http://notatrealserver.com/")) // URL does not matter
                    {
                        Assert.Equal(response.StatusCode, HttpStatusCode.OK);
                    }
                }
            }, async server =>
            {
                if (!IsCurlHandler) // libcurl sends Basic auth preemptively when only basic creds are provided; other handlers wait for 407.
                {
                    await server.AcceptConnectionSendResponseAndCloseAsync(
                        HttpStatusCode.ProxyAuthenticationRequired, "Proxy-Authenticate: Basic\r\n");
                }

                List<string> headers = await server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.OK);
                Assert.Equal(expectCreds, LoopbackServer.GetRequestHeaderValue(headers, "Proxy-Authorization"));
            });
        }

        // The purpose of this test is mainly to validate the .NET Framework OOB System.Net.Http implementation
        // since it has an underlying dependency to WebRequest. While .NET Core implementations of System.Net.Http
        // are not using any WebRequest code, the test is still useful to validate correctness.
        [OuterLoop("Uses external server")]
        [Fact]
        public async Task ProxyNotExplicitlyProvided_DefaultCredentialsSet_DefaultWebProxySetToNull_Success()
        {
            WebRequest.DefaultWebProxy = null;

            using (StandardSocketsHttpHandler handler = CreateSocketsHttpHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.DefaultProxyCredentials = new NetworkCredential("UsernameNotUsed", "PasswordNotUsed");
                HttpResponseMessage response = await client.GetAsync(Configuration.Http.RemoteEchoServer);

                Assert.Equal(response.StatusCode, HttpStatusCode.OK);
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract class HttpClientHandler_ClientCertificates_Test : HttpClientTestBase
    {
        public HttpClientHandler_ClientCertificates_Test(ITestOutputHelper output)
        {
            _output = output;
        }

        private readonly ITestOutputHelper _output;
        [Fact]
        public void ClientCertificateOptions_Default()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                Assert.Equal(ClientCertificateOption.Manual, handler.ClientCertificateOptions);
            }
        }

        [Theory]
        [InlineData((ClientCertificateOption)2)]
        [InlineData((ClientCertificateOption)(-1))]
        public void ClientCertificateOptions_InvalidArg_ThrowsException(ClientCertificateOption option)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.ClientCertificateOptions = option);
            }
        }

        [Theory]
        [InlineData(ClientCertificateOption.Automatic)]
        [InlineData(ClientCertificateOption.Manual)]
        public void ClientCertificateOptions_ValueArg_Roundtrips(ClientCertificateOption option)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                handler.ClientCertificateOptions = option;
                Assert.Equal(option, handler.ClientCertificateOptions);
            }
        }

        [Fact]
        public void ClientCertificates_ClientCertificateOptionsAutomatic_ThrowsException()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Automatic;
                Assert.Throws<InvalidOperationException>(() => handler.ClientCertificates);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task Automatic_SSLBackendNotSupported_ThrowsPlatformNotSupportedException()
        {
            if (BackendSupportsCustomCertificateHandling) // can't use [Conditional*] right now as it's evaluated at the wrong time for SocketsHttpHandler
            {
                return;
            }

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Automatic;
                await Assert.ThrowsAsync<PlatformNotSupportedException>(() => client.GetAsync(Configuration.Http.SecureRemoteEchoServer));
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task Manual_SSLBackendNotSupported_ThrowsPlatformNotSupportedException()
        {
            if (BackendSupportsCustomCertificateHandling) // can't use [Conditional*] right now as it's evaluated at the wrong time for SocketsHttpHandler
            {
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ClientCertificates.Add(Configuration.Certificates.GetClientCertificate());
            using (var client = new HttpClient(handler))
            {
                await Assert.ThrowsAsync<PlatformNotSupportedException>(() => client.GetAsync(Configuration.Http.SecureRemoteEchoServer));
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "dotnet/corefx #20010")]
        [ActiveIssue(9543)] // fails sporadically with 'WinHttpException : The server returned an invalid or unrecognized response' or 'TaskCanceledException : A task was canceled'
        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(6, false)]
        [InlineData(3, true)]
        public async Task Manual_CertificateSentMatchesCertificateReceived_Success(
            int numberOfRequests,
            bool reuseClient) // validate behavior with and without connection pooling, which impacts client cert usage
        {
            if (!BackendSupportsCustomCertificateHandling) // can't use [Conditional*] right now as it's evaluated at the wrong time for SocketsHttpHandler
            {
                _output.WriteLine($"Skipping {nameof(Manual_CertificateSentMatchesCertificateReceived_Success)}()");
                return;
            }

            if (!UseSocketsHttpHandler)
            {
                // Issue #9543: fails sporadically on WinHttpHandler/CurlHandler
                return;
            }

            var options = new LoopbackServer.Options { UseSsl = true };

            Func<X509Certificate2, HttpClient> createClient = (cert) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = delegate { return true; };
                handler.ClientCertificates.Add(cert);
                Assert.True(handler.ClientCertificates.Contains(cert));
                return new HttpClient(handler);
            };

            Func<HttpClient, LoopbackServer, Uri, X509Certificate2, Task> makeAndValidateRequest = async (client, server, url, cert) =>
            {
                await TestHelper.WhenAllCompletedOrAnyFailed(
                    client.GetStringAsync(url),
                    server.AcceptConnectionAsync(async connection =>
                    {
                        SslStream sslStream = Assert.IsType<SslStream>(connection.Stream);
                        Assert.Equal(cert, sslStream.RemoteCertificate);
                        await connection.ReadRequestHeaderAndSendResponseAsync();
                    }));
            };

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (X509Certificate2 cert = Configuration.Certificates.GetClientCertificate())
                {
                    if (reuseClient)
                    {
                        using (HttpClient client = createClient(cert))
                        {
                            for (int i = 0; i < numberOfRequests; i++)
                            {
                                await makeAndValidateRequest(client, server, url, cert);

                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < numberOfRequests; i++)
                        {
                            using (HttpClient client = createClient(cert))
                            {
                                await makeAndValidateRequest(client, server, url, cert);
                            }

                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }
                    }
                }
            }, options);
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(ClientCertificateOption.Manual)]
        [InlineData(ClientCertificateOption.Automatic)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Fails with \"Authentication failed\" error.")]
        public async Task AutomaticOrManual_DoesntFailRegardlessOfWhetherClientCertsAreAvailable(ClientCertificateOption mode)
        {
            if (!BackendSupportsCustomCertificateHandling) // can't use [Conditional*] right now as it's evaluated at the wrong time for SocketsHttpHandler
            {
                _output.WriteLine($"Skipping {nameof(Manual_CertificateSentMatchesCertificateReceived_Success)}()");
                return;
            }

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.ServerCertificateCustomValidationCallback = delegate { return true; };
                handler.ClientCertificateOptions = mode;

                await LoopbackServer.CreateServerAsync(async server =>
                {
                    Task clientTask = client.GetStringAsync(server.Uri);
                    Task serverTask = server.AcceptConnectionAsync(async connection =>
                    {
                        SslStream sslStream = Assert.IsType<SslStream>(connection.Stream);
                        await connection.ReadRequestHeaderAndSendResponseAsync();
                    });

                    await new Task[] { clientTask, serverTask }.WhenAllOrAnyFailed();
                }, new LoopbackServer.Options { UseSsl = true });
            }
        }

        private bool BackendSupportsCustomCertificateHandling
        {
            get
            {
#if TargetsWindows
                return true;
#else
                return TestHelper.NativeHandlerSupportsSslConfiguration();
#endif
            }
        }
    }
}

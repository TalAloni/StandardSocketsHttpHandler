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

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task Manual_SSLBackendNotSupported_ThrowsPlatformNotSupportedException()
        {
            if (BackendSupportsCustomCertificateHandling) // can't use [Conditional*] right now as it's evaluated at the wrong time for SocketsHttpHandler
            {
                return;
            }

            StandardSocketsHttpHandler handler = CreateSocketsHttpHandler();
            handler.SslOptions.ClientCertificates = new X509CertificateCollection();
            handler.SslOptions.ClientCertificates.Add(Configuration.Certificates.GetClientCertificate());
            handler.SslOptions.LocalCertificateSelectionCallback = (object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers) => handler.SslOptions.ClientCertificates[0];
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
                StandardSocketsHttpHandler handler = CreateSocketsHttpHandler();
                handler.SslOptions.RemoteCertificateValidationCallback = SecurityHelper.AllowAllCertificates;
                handler.SslOptions.ClientCertificates = new X509CertificateCollection();
                handler.SslOptions.ClientCertificates.Add(cert);
                Assert.True(handler.SslOptions.ClientCertificates.Contains(cert));
                handler.SslOptions.LocalCertificateSelectionCallback = (object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers) => cert;
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
        [Fact]
        public async Task AutomaticOrManual_DoesntFailRegardlessOfWhetherClientCertsAreAvailable()
        {
            if (!BackendSupportsCustomCertificateHandling) // can't use [Conditional*] right now as it's evaluated at the wrong time for SocketsHttpHandler
            {
                _output.WriteLine($"Skipping {nameof(Manual_CertificateSentMatchesCertificateReceived_Success)}()");
                return;
            }

            using (StandardSocketsHttpHandler handler = CreateSocketsHttpHandler())
            using (var client = new HttpClient(handler))
            {
                handler.SslOptions.RemoteCertificateValidationCallback = SecurityHelper.AllowAllCertificates;

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

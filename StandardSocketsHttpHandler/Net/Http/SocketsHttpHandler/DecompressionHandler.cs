// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed class DecompressionHandler : StandardHttpMessageHandler
    {
        private readonly StandardHttpMessageHandler _innerHandler;
        private readonly DecompressionMethods _decompressionMethods;

        private const string s_gzip = "gzip";
        private const string s_deflate = "deflate";
        private const string s_brotli = "br";
        private static readonly StringWithQualityHeaderValue s_gzipHeaderValue = new StringWithQualityHeaderValue(s_gzip);
        private static readonly StringWithQualityHeaderValue s_deflateHeaderValue = new StringWithQualityHeaderValue(s_deflate);
        private static readonly StringWithQualityHeaderValue s_brotliHeaderValue = new StringWithQualityHeaderValue(s_brotli);

        public DecompressionHandler(DecompressionMethods decompressionMethods, StandardHttpMessageHandler innerHandler)
        {
            Debug.Assert(decompressionMethods != DecompressionMethods.None);
            Debug.Assert(innerHandler != null);

            _decompressionMethods = decompressionMethods;
            _innerHandler = innerHandler;
        }

        internal bool GZipEnabled => (_decompressionMethods & DecompressionMethods.GZip) != 0;
        internal bool DeflateEnabled => (_decompressionMethods & DecompressionMethods.Deflate) != 0;
#if BROTLI
        internal bool BrotliEnabled => (_decompressionMethods & (DecompressionMethods)4) != 0;
#endif

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (GZipEnabled && !request.Headers.AcceptEncoding.Contains(s_gzipHeaderValue))
            {
                request.Headers.AcceptEncoding.Add(s_gzipHeaderValue);
            }

            if (DeflateEnabled && !request.Headers.AcceptEncoding.Contains(s_deflateHeaderValue))
            {
                request.Headers.AcceptEncoding.Add(s_deflateHeaderValue);
            }

#if BROTLI
            if (BrotliEnabled && !request.Headers.AcceptEncoding.Contains(s_brotliHeaderValue))
            {
                request.Headers.AcceptEncoding.Add(s_brotliHeaderValue);
            }
#endif

            HttpResponseMessage response = await _innerHandler.SendRequestAsync(request, cancellationToken).ConfigureAwait(false);

            ICollection<string> contentEncodings = response.Content.Headers.ContentEncoding;
            if (contentEncodings.Count > 0)
            {
                string last = null;
                foreach (string encoding in contentEncodings)
                {
                    last = encoding;
                }

                if (GZipEnabled && last == s_gzip)
                {
                    response.Content = new GZipDecompressedContent(response.Content);
                }
                else if (DeflateEnabled && last == s_deflate)
                {
                    response.Content = new DeflateDecompressedContent(response.Content);
                }
#if BROTLI
                else if (BrotliEnabled && last == s_brotli)
                {
                    response.Content = new BrotliDecompressedContent(response.Content);
                }
#endif
            }

            return response;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerHandler.Dispose();
            }

            base.Dispose(disposing);
        }

        private abstract class DecompressedContent : HttpContent
        {
            HttpContent _originalContent;
            bool _contentConsumed;

            public DecompressedContent(HttpContent originalContent)
            {
                _originalContent = originalContent;
                _contentConsumed = false;

                // Copy original response headers, but with the following changes:
                //   Content-Length is removed, since it no longer applies to the decompressed content
                //   The last Content-Encoding is removed, since we are processing that here.
                Headers.AddHeaders(originalContent.Headers);
                Headers.ContentLength = null;
                Headers.ContentEncoding.Clear();
                string prevEncoding = null;
                foreach (string encoding in originalContent.Headers.ContentEncoding)
                {
                    if (prevEncoding != null)
                    {
                        Headers.ContentEncoding.Add(prevEncoding);
                    }
                    prevEncoding = encoding;
                }
            }

            protected abstract Stream GetDecompressedStream(Stream originalStream);

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) =>
                SerializeToStreamAsyncInternal(stream, context, CancellationToken.None);

            internal async Task SerializeToStreamAsyncInternal(Stream stream, TransportContext context, CancellationToken cancellationToken)
            {
                using (Stream decompressedStream = await CreateContentReadStreamAsync().ConfigureAwait(false))
                {
                    await decompressedStream.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
                }
            }

            protected override async Task<Stream> CreateContentReadStreamAsync()
            {
                if (_contentConsumed)
                {
                    throw new InvalidOperationException(SR.net_http_content_stream_already_read);
                }

                _contentConsumed = true;

                Stream originalStream = await _originalContent.ReadAsStreamAsync().ConfigureAwait(false);
                return GetDecompressedStream(originalStream);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _originalContent.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        private sealed class GZipDecompressedContent : DecompressedContent
        {
            public GZipDecompressedContent(HttpContent originalContent)
                : base(originalContent)
            { }

            protected override Stream GetDecompressedStream(Stream originalStream) =>
                new GZipStream(originalStream, CompressionMode.Decompress);
        }

        private sealed class DeflateDecompressedContent : DecompressedContent
        {
            public DeflateDecompressedContent(HttpContent originalContent)
                : base(originalContent)
            { }

            protected override Stream GetDecompressedStream(Stream originalStream) =>
                new DeflateStream(originalStream, CompressionMode.Decompress);
        }

#if BROTLI
        private sealed class BrotliDecompressedContent : DecompressedContent
        {
            public BrotliDecompressedContent(HttpContent originalContent) :
                base(originalContent)
            {
            }

            protected override Stream GetDecompressedStream(Stream originalStream) =>
                new BrotliStream(originalStream, CompressionMode.Decompress);
        }
#endif
    }
}

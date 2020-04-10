
namespace System.Net.Http
{
    internal class HttpRetryableRequestException : HttpRequestException
    {
        internal bool AllowRetry { get; }

        public HttpRetryableRequestException(string message, Exception inner, bool allowRetry) : base(message, inner)
        {
            AllowRetry = allowRetry;
        }
    }
}

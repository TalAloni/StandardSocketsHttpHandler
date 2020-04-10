using System;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public abstract class StandardHttpMessageHandler : HttpMessageHandler
    {
        public virtual Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return this.SendAsync(request, cancellationToken);
        }
    }
}

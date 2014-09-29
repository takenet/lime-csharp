using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Http
{
    public interface IRequestProcessor
    {
        string[] Methods { get; }

        UriTemplate Template { get; }

        Task ProcessAsync(HttpListenerContext context, ServerHttpTransport transport, CancellationToken cancellationToken);
    }
}

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
        HashSet<string> Methods { get; }

        UriTemplate Template { get; }

        Task ProcessAsync(HttpListenerContext context, ServerHttpTransport transport, UriTemplateMatch match, CancellationToken cancellationToken);
    }
}

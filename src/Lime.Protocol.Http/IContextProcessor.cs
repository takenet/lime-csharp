using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Http
{
    /// <summary>
    /// Represents a HTTP listener
    /// context processor.
    /// </summary>
    public interface IContextProcessor
    {
        /// <summary>
        /// Gets the supported HTTP methods.
        /// </summary>
        HashSet<string> Methods { get; }

        /// <summary>
        /// Gets the URI template
        /// for the context requests.
        /// </summary>
        UriTemplate Template { get; }

        /// <summary>
        /// Process the HTTP listener context.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="transport"></param>
        /// <param name="match"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ProcessAsync(HttpListenerContext context, ServerHttpTransport transport, UriTemplateMatch match, CancellationToken cancellationToken);
    }
}
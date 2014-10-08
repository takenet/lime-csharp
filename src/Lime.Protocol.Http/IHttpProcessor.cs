using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Http
{
    /// <summary>
    /// Defines a processor
    /// for HTTP requests.
    /// </summary>
    public interface IHttpProcessor
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
        /// Processes the HTTP request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="match">The match.</param>
        /// <param name="transport">The transport.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<HttpResponse> ProcessAsync(HttpRequest request, UriTemplateMatch match, ITransportSession transport, CancellationToken cancellationToken);
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Http
{
    /// <summary>
    /// Defines a basic HTTP server.
    /// </summary>
    public interface IHttpServer
    {
        /// <summary>
        /// Starts listening for requests.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the listener.
        /// </summary>
        void Stop();

        /// <summary>
        /// Awaits for a HTTP request.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<HttpRequest> AcceptRequestAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Submits a HTTP response.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns></returns>
        Task SubmitResponseAsync(HttpResponse response);
    }
}
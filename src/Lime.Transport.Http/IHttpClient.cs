using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Transport.Http
{
    /// <summary>
    /// Defines a service for sending HTTP requests to a server.
    /// </summary>
    public interface IHttpClient
    {
        /// <summary>
        /// Sends an HTTP request and awaits for the response.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
    }
}
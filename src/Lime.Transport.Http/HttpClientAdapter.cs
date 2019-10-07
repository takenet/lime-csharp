using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Transport.Http
{
    public class HttpClientAdapter : IHttpClient
    {
        private readonly HttpClient _httpClient;

        public HttpClientAdapter(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _httpClient.SendAsync(request, cancellationToken);
        }
    }

}
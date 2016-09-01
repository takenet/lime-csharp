using System;
using System.Collections.Concurrent;
using System.Net;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;

namespace Lime.Transport.Http
{
    /// <summary>
    /// Wrapper around the <see cref="HttpListener"/> class.
    /// </summary>
    /// <seealso cref="Lime.Transport.Http.IHttpServer" />
    /// <seealso cref="System.IDisposable" />
    public sealed class HttpServer : IHttpServer, IDisposable
    {
        private readonly TimeSpan? _requestTimeout;
        private readonly HttpListener _httpListener;
        private readonly MemoryCache _memoryCache;
        private readonly CacheItemPolicy _cacheItemPolicy;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpServer"/> class.
        /// </summary>
        /// <param name="prefixes">The prefixes.</param>
        /// <param name="authenticationSchemes">The authentication schemes.</param>
        /// <param name="requestTimeout">The request timeout.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException">Windows XP SP2 or Server 2003 is required to use the HttpListener class.</exception>
        public HttpServer(
            string[] prefixes, 
            AuthenticationSchemes authenticationSchemes, 
            TimeSpan? requestTimeout = null)
        {            
            if (prefixes == null) throw new ArgumentNullException(nameof(prefixes));
            if (!HttpListener.IsSupported)
            {
                throw new NotSupportedException("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
            }

            _httpListener = new HttpListener();
            foreach (var prefix in prefixes)
            {
                _httpListener.Prefixes.Add(prefix);
            }
            _httpListener.AuthenticationSchemes = authenticationSchemes;
            _requestTimeout = requestTimeout;
            _memoryCache = new MemoryCache(nameof(HttpRequest));
            _cacheItemPolicy = new CacheItemPolicy();
            if (_requestTimeout != null)
            {
                _cacheItemPolicy.RemovedCallback = OnContextTimeout;
                _cacheItemPolicy.SlidingExpiration = _requestTimeout.Value;
            }
        }

        public void Start()
        {
            try
            {
                _httpListener.Start();

            }
            catch (HttpListenerException ex)
            {
                throw new HttpServerException(ex.Message, ex);
            }
        }

        public void Stop()
        {
            _httpListener.Stop();
        }

        public async Task<HttpRequest> AcceptRequestAsync(CancellationToken cancellationToken)
        {
            try
            {
                var context = await _httpListener
                    .GetContextAsync()
                    .WithCancellation(cancellationToken)
                    .ConfigureAwait(false);

                Guid correlatorId;
                do
                {
                    correlatorId = Guid.NewGuid();
                } while (!_memoryCache.Add(correlatorId.ToString(), context, _cacheItemPolicy));

                MediaType contentType = null;
                if (!string.IsNullOrEmpty(context.Request.ContentType))
                {
                    contentType = MediaType.Parse(context.Request.ContentType);
                }

                return new HttpRequest(
                    context.Request.HttpMethod,
                    context.Request.Url,
                    context.User,
                    correlatorId,
                    (WebHeaderCollection)context.Request.Headers,
                    context.Request.QueryString,
                    contentType,
                    context.Request.InputStream);
            }
            catch (HttpListenerException ex)
            {
                if (ex.ErrorCode == 995)
                {
                    // Workarround since the GetContextAsync method doesn't supports cancellation
                    // "The I/O operation has been aborted because of either a thread exit or an application request"
                    throw new OperationCanceledException("The listener was cancelled", ex);
                }                

                throw new HttpServerException(ex.Message, ex);
            }        
        }

        public async Task SubmitResponseAsync(HttpResponse response)
        {
            var context = _memoryCache.Remove(response.CorrelatorId.ToString()) as HttpListenerContext;
            if (context == null)
            {
                throw new ArgumentException("Invalid response CorrelatorId", nameof(response));
            }

            context.Response.StatusCode = (int)response.StatusCode;
            if (!string.IsNullOrWhiteSpace(response.StatusDescription))
            {
                context.Response.StatusDescription = response.StatusDescription;
            }
            
            context.Response.Headers = response.Headers;

            if (response.BodyStream != null)
            {
                await response.BodyStream.CopyToAsync(context.Response.OutputStream).ConfigureAwait(false);                
            }
            
            context.Response.Close();
        }

        public void Dispose()
        {
            _memoryCache.Dispose();
            _httpListener.DisposeIfDisposable();
        }

        private void OnContextTimeout(CacheEntryRemovedArguments arguments)
        {
            if (arguments.RemovedReason != CacheEntryRemovedReason.Removed)
            {
                var context = (HttpListenerContext) arguments.CacheItem.Value;
                context.Response.StatusCode = (int) HttpStatusCode.GatewayTimeout;
                context.Response.Close();
            }
        }
    }
}

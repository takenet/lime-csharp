using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace Lime.Protocol.Http
{
    public sealed class HttpServer : IHttpServer, IDisposable
    {
        private readonly HttpListener _httpListener;
        private readonly ConcurrentDictionary<Guid, HttpListenerContext> _pendingContextsDictionary;

        public HttpServer(string[] prefixes, AuthenticationSchemes authenticationSchemes)
        {
            _httpListener = new HttpListener();
            foreach (var prefix in prefixes)
            {
                _httpListener.Prefixes.Add(prefix);
            }
            _httpListener.AuthenticationSchemes = authenticationSchemes;

            _pendingContextsDictionary = new ConcurrentDictionary<Guid, HttpListenerContext>();
        }

        #region IHttpServer Members

        public void Start()
        {
            _httpListener.Start();
        }

        public void Stop()
        {
            _httpListener.Stop();
        }

        public async Task<HttpRequest> AcceptRequestAsync(CancellationToken cancellationToken)
        {
            var context = await _httpListener.GetContextAsync().WithCancellation(cancellationToken).ConfigureAwait(false);

            Guid correlatorId;
            if (!Guid.TryParse(context.Request.Headers.Get(Constants.ENVELOPE_ID_HEADER), out correlatorId) ||
                correlatorId == Guid.Empty)
            {
                correlatorId = Guid.NewGuid();
            }

            while (!_pendingContextsDictionary.TryAdd(correlatorId, context))
            {
                correlatorId = Guid.NewGuid();
            }

            return new HttpRequest(
                context.Request.HttpMethod,
                context.Request.Url,
                context.User,
                correlatorId,
                (WebHeaderCollection)context.Request.Headers,
                context.Request.QueryString,
                context.Request.InputStream);            
        }

        public async Task SubmitResponseAsync(HttpResponse response, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            HttpListenerContext context;

            if (!_pendingContextsDictionary.TryRemove(response.CorrelatorId, out context))
            {
                throw new ArgumentException("Invalid response CorrelatorId", "response");
            }

            context.Response.StatusCode = (int)response.StatusCode;
            if (!string.IsNullOrWhiteSpace(response.StatusDescription))
            {
                context.Response.StatusDescription = response.StatusDescription;
            }
            
            context.Response.Headers = response.Headers;

            if (response.Body != null)
            {
                using (var writer = new StreamWriter(context.Response.OutputStream))
                {
                    await writer.WriteAsync(response.Body).ConfigureAwait(false);
                }
            }
            
            context.Response.Close();
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            _httpListener.DisposeIfDisposable();
        }

        #endregion
    }
}

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
        #region Private Fields

        private readonly HttpListener _httpListener;
        private readonly ConcurrentDictionary<Guid, HttpListenerContext> _pendingContextsDictionary;

        #endregion

        #region Constructor

        public HttpServer(string[] prefixes, AuthenticationSchemes authenticationSchemes)
        {
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
            _pendingContextsDictionary = new ConcurrentDictionary<Guid, HttpListenerContext>();
        }

        #endregion

        #region IHttpServer Members

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
                if (!Guid.TryParse(context.Request.GetValue(Constants.ENVELOPE_ID_HEADER, Constants.ENVELOPE_ID_QUERY), out correlatorId) ||
                    correlatorId == Guid.Empty)
                {
                    correlatorId = context.Request.RequestTraceIdentifier;
                }

                while (!_pendingContextsDictionary.TryAdd(correlatorId, context))
                {
                    correlatorId = Guid.NewGuid();
                }

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

            if (response.BodyStream != null)
            {
                await response.BodyStream.CopyToAsync(context.Response.OutputStream);                
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

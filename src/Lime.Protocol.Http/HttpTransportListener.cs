using Lime.Protocol.Http.Processors;
using Lime.Protocol.Http.Serialization;
using Lime.Protocol.Http.Storage;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using Lime.Protocol.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Lime.Protocol.Http
{
    public sealed class HttpTransportListener : ITransportListener, IDisposable
    {
        private readonly bool _useHttps;       
        private readonly TimeSpan _requestTimeout;
        private readonly bool _writeExceptionsToOutput;        

        private readonly UriTemplateTable _uriTemplateTable;

        private readonly IDocumentSerializer _serializer;
        private readonly IEnvelopeStorage<Message> _messageStorage;
        private readonly IEnvelopeStorage<Notification> _notificationStorage;
        private readonly ITraceWriter _traceWriter;

        private readonly IHttpTransportProvider _httpTransportProvider;
        private readonly BufferBlock<ServerHttpTransport> _transportBufferBlock;

        private readonly IHttpServer _httpServer;        

        private readonly BufferBlock<HttpRequest> _httpRequestBufferBlock;
        private readonly TransformBlock<HttpRequest, HttpResponse> _processHttpRequestBufferBlock;
        private readonly ActionBlock<HttpResponse> _httpResponseActionBlock;

        private CancellationTokenSource _listenerCancellationTokenSource;
        private Task _httpServerListenerTask;
                
        /// <summary>
        /// Creates a new instance of the
        /// </summary>
        /// <param name="port"></param>
        /// <param name="hostName"></param>
        /// <param name="useHttps"></param>
        /// <param name="requestTimeout"></param>
        /// <param name="writeExceptionsToOutput"></param>
        /// <param name="serializer"></param>
        /// <param name="messageStorage"></param>
        /// <param name="notificationStorage"></param>
        /// <param name="traceWriter"></param>
        public HttpTransportListener(int port, string hostName = "*", bool useHttps = false, TimeSpan requestTimeout = default(TimeSpan), bool writeExceptionsToOutput = true, 
            IDocumentSerializer serializer = null, IHttpServer httpServer = null, IHttpTransportProvider httpTransportProvider = null, IEnvelopeStorage<Message> messageStorage = null, IEnvelopeStorage<Notification> notificationStorage = null, ITraceWriter traceWriter = null)
        {
            if (!HttpListener.IsSupported)
            {
                throw new NotSupportedException("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
            }

            _useHttps = useHttps;
            var scheme = _useHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;

            _writeExceptionsToOutput = writeExceptionsToOutput;
            _requestTimeout = requestTimeout != default(TimeSpan) ? requestTimeout : TimeSpan.FromSeconds(Constants.DEFAULT_REQUEST_TIMEOUT);

            var basePath = string.Format("{0}://{1}:{2}", scheme, hostName, port);
            var prefixes = new string[]
            {
                Constants.ROOT + Constants.MESSAGES_PATH + Constants.ROOT,
                Constants.ROOT + Constants.COMMANDS_PATH + Constants.ROOT,
                Constants.ROOT + Constants.NOTIFICATIONS_PATH + Constants.ROOT
            };

            var fullPrefixes = prefixes.Select(p => basePath + p).ToArray();

            var safeHostName = hostName;
            if (hostName.Equals("*") || hostName.Equals("+"))
            {
                safeHostName = "localhost";
            }

            var baseUri = new Uri(string.Format("{0}://{1}:{2}", scheme, safeHostName, port));
            ListenerUris = prefixes
                .Select(p => new Uri(baseUri, p))
                .ToArray();

            _httpServer = httpServer ?? new HttpServer(fullPrefixes, AuthenticationSchemes.Basic);
            _serializer = serializer ?? new DocumentSerializer();
            _messageStorage = messageStorage ?? new DictionaryEnvelopeStorage<Message>();
            _notificationStorage = notificationStorage ?? new DictionaryEnvelopeStorage<Notification>();
            _traceWriter = traceWriter;
            _httpTransportProvider = httpTransportProvider ?? new HttpTransportProvider(_useHttps, _messageStorage, _notificationStorage);
            _httpTransportProvider.TransportCreated += async (sender, e) => await _transportBufferBlock.SendAsync(e.Transport, _listenerCancellationTokenSource.Token).ConfigureAwait(false);

            // Pipeline
            _transportBufferBlock = new BufferBlock<ServerHttpTransport>();

            _httpRequestBufferBlock = new BufferBlock<HttpRequest>();
            _processHttpRequestBufferBlock = new TransformBlock<HttpRequest, HttpResponse>(r => ProcessAsync(r));
            _httpResponseActionBlock = new ActionBlock<HttpResponse>(r => SubmitResponseAsync(r));
            _httpRequestBufferBlock.LinkTo(_processHttpRequestBufferBlock);
            _processHttpRequestBufferBlock.LinkTo(_httpResponseActionBlock);

            // Context processors
            _uriTemplateTable = new UriTemplateTable(baseUri);
            RegisterProcessors();
        }

        #region ITransportListener Members

        public Uri[] ListenerUris { get; private set; }


        public Task StartAsync()
        {
            if (_httpServerListenerTask != null)
            {
                throw new InvalidOperationException("The listener is already started.");
            }

            _listenerCancellationTokenSource = new CancellationTokenSource();
            _httpServer.Start();
            _httpServerListenerTask = ListenAsync();
            return Task.FromResult<object>(null);            
        }

        public async Task<ITransport> AcceptTransportAsync(CancellationToken cancellationToken)
        {
            if (_httpServerListenerTask == null)
            {
                throw new InvalidOperationException("The listener was not started.");
            }

            var linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _listenerCancellationTokenSource.Token);

            return await _transportBufferBlock.ReceiveAsync(linkedCancellationToken.Token).ConfigureAwait(false);
        }        

        public async Task StopAsync()
        {
            if (_httpServerListenerTask == null)
            {
                throw new InvalidOperationException("The listener was not started.");
            }

            _httpServer.Stop();
            _listenerCancellationTokenSource.Cancel();
            await _httpServerListenerTask.ConfigureAwait(false);
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            _httpServer.DisposeIfDisposable();
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Consumes the requests
        /// from the HTTP server.
        /// </summary>
        /// <returns></returns>
        private async Task ListenAsync()
        {
            try
            {
                while (!_listenerCancellationTokenSource.IsCancellationRequested)
                {
                    var request = await _httpServer
                        .AcceptRequestAsync(_listenerCancellationTokenSource.Token)
                        .ConfigureAwait(false);

                    if (!await _httpRequestBufferBlock.SendAsync(request, _listenerCancellationTokenSource.Token).ConfigureAwait(false))
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task<HttpResponse> ProcessAsync(HttpRequest request)
        {
            HttpResponse response = null;

            Exception exception = null;

            try
            {
                var cancellationToken = _requestTimeout.ToCancellationToken();

                var match = _uriTemplateTable
                    .Match(request.Uri)
                    .Where(m => ((IHttpProcessor)m.Data).Methods.Contains(request.Method))
                    .FirstOrDefault();

                if (match != null)
                {
                    // Authenticate the request session
                    var transport = _httpTransportProvider.GetTransport(request.User);
                    var session = await transport.AuthenticateAsync(cancellationToken).ConfigureAwait(false);
                    if (session.State == SessionState.Established)
                    {
                        var processor = (IHttpProcessor)match.Data;
                        response = await processor.ProcessAsync(request, match, transport, cancellationToken).ConfigureAwait(false);
                    }
                    else if (session.Reason != null)
                    {
                        response = new HttpResponse(
                            request.CorrelatorId,
                            session.Reason.ToHttpStatusCode(),
                            session.Reason.Description);
                    }
                    else
                    {
                        response = new HttpResponse(
                            request.CorrelatorId,
                            HttpStatusCode.ServiceUnavailable);
                    }
                }
                else
                {
                    response = new HttpResponse(
                        request.CorrelatorId,
                        HttpStatusCode.NotFound);
                }
            }
            catch (LimeException ex)
            {
                response = new HttpResponse(
                    request.CorrelatorId,
                    ex.Reason.ToHttpStatusCode(),
                    ex.Reason.Description);
            }
            catch (OperationCanceledException)
            {
                response = new HttpResponse(
                    request.CorrelatorId,
                    HttpStatusCode.RequestTimeout);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception != null)
            {
                response = new HttpResponse(
                    request.CorrelatorId,
                    HttpStatusCode.InternalServerError);
            }

            return response;
        }

        private async Task SubmitResponseAsync(HttpResponse response)
        {
            Exception exception = null;

            try
            {
                await _httpServer.SubmitResponseAsync(response, _listenerCancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception != null)
            {

            }
        }


        /// <summary>
        /// Register the context processors
        /// in the URI template table.
        /// </summary>
        private void RegisterProcessors()
        {
            var sendCommandContextProcessor = new SendCommandHttpProcessor(_traceWriter);
            var sendMessageContextProcessor = new SendMessageHttpProcessor(_traceWriter);
            var getMessagesContextProcessor = new GetMessagesHttpProcessor(_messageStorage);
            var getMessageByIdContextProcessor = new GetMessageByIdHttpProcessor(_messageStorage);
            var deleteMessageByIdContextProcessor = new DeleteMessageByIdContextProcessor(_messageStorage);
            var sendNotificationContextProcessor = new SendNotificationHttpProcessor(_traceWriter);
            var getNotificationsContextProcessor = new GetNotificationsHttpProcessor(_notificationStorage);
            var getNotificationByIdContextProcessor = new GetNotificationByIdHttpProcessor(_notificationStorage);
            var deleteNotificationByIdContextProcessor = new DeleteNotificationByIdHttpProcessor(_notificationStorage);
            _uriTemplateTable.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(sendCommandContextProcessor.Template, sendCommandContextProcessor));
            _uriTemplateTable.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(sendMessageContextProcessor.Template, sendMessageContextProcessor));
            _uriTemplateTable.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(getMessagesContextProcessor.Template, getMessagesContextProcessor));
            _uriTemplateTable.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(getMessageByIdContextProcessor.Template, getMessageByIdContextProcessor));
            _uriTemplateTable.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(deleteMessageByIdContextProcessor.Template, deleteMessageByIdContextProcessor));
            _uriTemplateTable.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(sendNotificationContextProcessor.Template, sendNotificationContextProcessor));
            _uriTemplateTable.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(getNotificationsContextProcessor.Template, getNotificationsContextProcessor));
            _uriTemplateTable.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(getNotificationByIdContextProcessor.Template, getNotificationByIdContextProcessor));
            _uriTemplateTable.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(deleteNotificationByIdContextProcessor.Template, deleteNotificationByIdContextProcessor));
            _uriTemplateTable.MakeReadOnly(true);
        }

        #endregion
    }
}

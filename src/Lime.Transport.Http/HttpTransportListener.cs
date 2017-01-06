using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using Lime.Transport.Http.Processors;
using Lime.Transport.Http.Storage;

namespace Lime.Transport.Http
{
    /// <summary>
    /// Listens for HTTP requests emulating the
    /// behavior of LIME in the backend.
    /// </summary>
    public sealed class HttpTransportListener : ITransportListener, IDisposable
    {
        private readonly bool _useHttps;
        private readonly TimeSpan _requestTimeout;
        private readonly bool _writeExceptionsToOutput;
        private readonly int _maxDegreeOfParallelism;
        private readonly IHttpServer _httpServer;
        private readonly IHttpTransportProvider _httpTransportProvider;
        private readonly IDocumentSerializer _serializer;
        private readonly IEnvelopeStorage<Message> _messageStorage;
        private readonly IEnvelopeStorage<Notification> _notificationStorage;
        private readonly ITraceWriter _traceWriter;
        private readonly UriTemplateTable _uriTemplateTable;

        private BufferBlock<ITransport> _transportBufferBlock;        
        private BufferBlock<HttpRequest> _httpRequestBufferBlock;
        private TransformBlock<HttpRequest, HttpResponse> _processHttpRequestBufferBlock;
        private ActionBlock<HttpResponse> _httpResponseActionBlock;
        
        private CancellationTokenSource _listenerCancellationTokenSource;
        private Task _httpServerListenerTask;

        /// <summary>
        /// Creates a new instance of the HttpTransportListener class.
        /// </summary>
        /// <param name="port">The port for listening.</param>
        /// <param name="hostName">Name of the host for binding.</param>
        /// <param name="useHttps">if set to <c>true</c> the listener endpoint will use HTTPS.</param>
        /// <param name="requestTimeout">The request timeout.</param>
        /// <param name="writeExceptionsToOutput">if set to <c>true</c> the exceptions details will be written in the response body.</param>
        /// <param name="httpServer">The HTTP server instance.</param>
        /// <param name="httpTransportProvider">The HTTP transport provider instance.</param>
        /// <param name="serializer">The serializer instance.</param>
        /// <param name="messageStorage">The message storage instance.</param>
        /// <param name="notificationStorage">The notification storage instance.</param>
        /// <param name="processors">The processors.</param>
        /// <param name="traceWriter">The trace writer.</param>
        public HttpTransportListener(int port, string hostName = "*", bool useHttps = false, TimeSpan requestTimeout = default(TimeSpan), TimeSpan transportExpirationInactivityInterval = default(TimeSpan), bool writeExceptionsToOutput = true,
            int maxDegreeOfParallelism = 1, IHttpServer httpServer = null, IHttpTransportProvider httpTransportProvider = null, IDocumentSerializer serializer = null, IEnvelopeStorage<Message> messageStorage = null,
            IEnvelopeStorage<Notification> notificationStorage = null, IHttpProcessor[] processors = null, ITraceWriter traceWriter = null)
        {
            _useHttps = useHttps;
            var scheme = _useHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;

            _writeExceptionsToOutput = writeExceptionsToOutput;
            _maxDegreeOfParallelism = maxDegreeOfParallelism;
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

            transportExpirationInactivityInterval = transportExpirationInactivityInterval != default(TimeSpan) ? transportExpirationInactivityInterval : TimeSpan.FromSeconds(Constants.DEFAULT_TRANSPORT_EXPIRATION_INACTIVITY_INTERVAL);
            _httpTransportProvider = httpTransportProvider ?? new HttpTransportProvider(_useHttps, _messageStorage, _notificationStorage, transportExpirationInactivityInterval);
            _httpTransportProvider.TransportCreated += async (sender, e) => await _transportBufferBlock.SendAsync(e.Transport, _listenerCancellationTokenSource.Token).ConfigureAwait(false);

            // Context processors
            _uriTemplateTable = new UriTemplateTable(baseUri);
            if (processors == null)
            {
                processors = CreateProcessors();
            }            
            foreach (var processor in processors)
            {
                _uriTemplateTable.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(processor.Template, processor));
            }
            _uriTemplateTable.MakeReadOnly(true);            
        }

        public Uri[] ListenerUris { get; private set; }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_httpServerListenerTask != null)
            {
                throw new InvalidOperationException("The listener is already started.");
            }

            _listenerCancellationTokenSource = new CancellationTokenSource();
            BuildPipeline();
            
            _httpServer.Start();
            _httpServerListenerTask = Task.Run(ListenAsync);
            return Task.FromResult<object>(null);
        }

        public async Task<ITransport> AcceptTransportAsync(CancellationToken cancellationToken)
        {
            if (_httpServerListenerTask == null)
            {
                throw new InvalidOperationException("The listener was not started.");
            }

            using (var linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _listenerCancellationTokenSource.Token))
            {
                return await _transportBufferBlock.ReceiveAsync(linkedCancellationToken.Token);
            }
        }        

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_httpServerListenerTask == null)
            {
                throw new InvalidOperationException("The listener was not started.");
            }

            _httpServer.Stop();
            _transportBufferBlock.Complete();
            _httpRequestBufferBlock.Complete();            
            
            _listenerCancellationTokenSource.Cancel();

            await Task.WhenAll(
                _httpServerListenerTask,
                _transportBufferBlock.Completion,
                _httpRequestBufferBlock.Completion).ConfigureAwait(false);

            _httpServerListenerTask = null;
        }

        public void Dispose()
        {
            _httpServer.DisposeIfDisposable();
            _listenerCancellationTokenSource.DisposeIfDisposable();
        }

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

        /// <summary>
        /// Processes the HTTP request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        private async Task<HttpResponse> ProcessAsync(HttpRequest request)
        {
            HttpResponse response;

            try
            {
                var match = _uriTemplateTable
                    .Match(request.Uri)
                    .FirstOrDefault(m => ((IHttpProcessor)m.Data).Methods.Contains(request.Method));

                if (match != null)
                {
                    using (var cts = new CancellationTokenSource(_requestTimeout))
                    {
                        var cancellationToken = cts.Token;

                        var closeSession = !string.IsNullOrEmpty(request.Headers.Get(Constants.SESSION_HEADER)) &&
                                           request.Headers.Get(Constants.SESSION_HEADER)
                                               .Equals(Constants.CLOSE_HEADER_VALUE, StringComparison.OrdinalIgnoreCase);

                        if (request.User != null)
                        {
                            var transport = _httpTransportProvider.GetTransport(request.User, !closeSession);

                            Exception exception = null;
                            try
                            {
                                // Authenticate the request session
                                var session = await transport.GetSessionAsync(cancellationToken).ConfigureAwait(false);
                                if (session.State == SessionState.Established)
                                {
                                    var processor = (IHttpProcessor) match.Data;
                                    response =
                                        await
                                            processor.ProcessAsync(request, match, transport, cancellationToken)
                                                .ConfigureAwait(false);
                                }
                                else if (session.Reason != null)
                                {
                                    response = new HttpResponse(request.CorrelatorId, session.Reason.ToHttpStatusCode(),
                                        session.Reason.Description);
                                }
                                else
                                {
                                    response = new HttpResponse(request.CorrelatorId, HttpStatusCode.ServiceUnavailable);
                                }

                                response.Headers.Add(Constants.SESSION_ID_HEADER, session.Id.ToString());
                            }
                            catch (Exception ex)
                            {
                                response = null;
                                exception = ex;
                            }

                            if (closeSession)
                            {
                                await
                                    transport.FinishAsync(_listenerCancellationTokenSource.Token).ConfigureAwait(false);
                            }
                            else if (response != null)
                            {
                                response.Headers.Add(Constants.SESSION_EXPIRATION_HEADER,
                                    transport.Expiration.ToString("r"));
                            }

                            if (exception != null)
                            {
                                throw exception;
                            }
                        }
                        else
                        {
                            response = new HttpResponse(request.CorrelatorId, HttpStatusCode.Unauthorized);
                        }
                    }
                }
                else
                {
                    response = new HttpResponse(request.CorrelatorId, HttpStatusCode.NotFound);
                }
            }
            catch (LimeException ex)
            {
                response = new HttpResponse(request.CorrelatorId, ex.Reason.ToHttpStatusCode(), ex.Reason.Description);
            }
            catch (OperationCanceledException)
            {
                response = new HttpResponse(request.CorrelatorId, HttpStatusCode.RequestTimeout);
            }
            catch (Exception ex)
            {
                string body = null;

                if (_writeExceptionsToOutput)
                {
                    body = ex.ToString();
                }

                response = new HttpResponse(request.CorrelatorId, HttpStatusCode.InternalServerError, body: body);
            }
           
            return response;
        }

        /// <summary>
        /// Submits the response 
        /// to the HTTP server.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns></returns>
        private async Task SubmitResponseAsync(HttpResponse response)
        {
            Exception exception = null;

            try
            {
                await _httpServer.SubmitResponseAsync(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception != null &&
                _traceWriter != null &&
                _traceWriter.IsEnabled)
            {
                await _traceWriter.TraceAsync("SubmitResponseAsync: " + exception.ToString(), DataOperation.Send).ConfigureAwait(false);
            }
        }

        private void BuildPipeline()
        {
            _transportBufferBlock = new BufferBlock<ITransport>();
            _httpRequestBufferBlock = new BufferBlock<HttpRequest>();
            var executionOptions = new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = _maxDegreeOfParallelism
            };
            _processHttpRequestBufferBlock = new TransformBlock<HttpRequest, HttpResponse>(r => ProcessAsync(r), executionOptions);
            _httpResponseActionBlock = new ActionBlock<HttpResponse>(r => SubmitResponseAsync(r), executionOptions);

            var linkOptions = new DataflowLinkOptions()
            {
                PropagateCompletion = true
            };

            _httpRequestBufferBlock.LinkTo(_processHttpRequestBufferBlock, linkOptions);
            _processHttpRequestBufferBlock.LinkTo(_httpResponseActionBlock, linkOptions);
        }

        /// <summary>
        /// Gets the context processors
        /// for the URI template table.
        /// </summary>
        private IHttpProcessor[] CreateProcessors()
        {
            return new IHttpProcessor[]
                {
                    new SendCommandHttpProcessor(traceWriter: _traceWriter),
                    new SendMessageHttpProcessor(_traceWriter),
                    new GetMessagesHttpProcessor(_messageStorage),
                    new GetMessageByIdHttpProcessor(_messageStorage),
                    new DeleteMessageByIdHttpProcessor(_messageStorage),
                    new SendNotificationHttpProcessor(_traceWriter),
                    new GetNotificationsHttpProcessor(_notificationStorage),
                    new GetNotificationByIdHttpProcessor(_notificationStorage),
                    new DeleteNotificationByIdHttpProcessor(_notificationStorage)
                };      
        }
    }
}

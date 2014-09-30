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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Lime.Protocol.Http
{
    /// <summary>
    /// Implements a HTTP listener server 
    /// that supports an emulation layer 
    /// for the LIME protocol.
    /// </summary>
    public sealed class HttpTransportListener : ITransportListener, IDisposable
    {
        #region Private Fields

        private readonly IDocumentSerializer _serializer;
        private readonly bool _useHttps;
        private readonly bool _writeExceptionsToOutput;
        private readonly TimeSpan _requestTimeout;
        private readonly BufferBlock<ServerHttpTransport> _transportBufferBlock;
        private readonly ConcurrentDictionary<string, ServerHttpTransport> _transportDictionary;
        private readonly BufferBlock<HttpListenerContext> _listenerInputBufferBlock;
        private readonly ActionBlock<HttpListenerContext> _processContextActionBlock;
        private readonly ConcurrentDictionary<Guid, HttpListenerResponse> _pendingResponsesDictionary;
        private readonly ActionBlock<Envelope> _processTransportOutputActionBlock;
        private readonly IEnvelopeStorage<Message> _messageStorage;
        private readonly IEnvelopeStorage<Notification> _notificationStorage;
        private readonly UriTemplateTable _uriTemplateTable;
        private readonly string _basePath;
        private readonly string[] _prefixes;
        private readonly ITraceWriter _traceWriter;

        private HttpListener _httpListener;
        private Task _listenerTask;
        private CancellationTokenSource _listenerCancellationTokenSource;

        #endregion        

        #region Constructor

        public HttpTransportListener(int port, string hostName = "*", bool useHttps = false, TimeSpan requestTimeout = default(TimeSpan), bool writeExceptionsToOutput = true, IDocumentSerializer serializer = null, IEnvelopeStorage<Message> messageStorage = null, IEnvelopeStorage<Notification> notificationStorage = null, ITraceWriter traceWriter = null)
        {
            if (!HttpListener.IsSupported)
            {
                throw new NotSupportedException("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
            }

            _useHttps = useHttps;

            var scheme = _useHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;                                   

            _basePath = string.Format("{0}://{1}:{2}", scheme, hostName, port);
            _prefixes = new string[]
            {
                Constants.ROOT + Constants.MESSAGES_PATH + Constants.ROOT,
                Constants.ROOT + Constants.COMMANDS_PATH + Constants.ROOT,
                Constants.ROOT + Constants.NOTIFICATIONS_PATH + Constants.ROOT
            };

            var safeHostName = hostName;
            if (hostName.Equals("*") || hostName.Equals("+"))
            {
                safeHostName = "localhost";
            }

            var baseUri = new Uri(string.Format("{0}://{1}:{2}", scheme, safeHostName, port));
            ListenerUris = _prefixes
                .Select(p => new Uri(baseUri, p))
                .ToArray();

            _requestTimeout = requestTimeout != default(TimeSpan) ? requestTimeout : TimeSpan.FromSeconds(Constants.DEFAULT_REQUEST_TIMEOUT);
            _writeExceptionsToOutput = writeExceptionsToOutput;            
            _serializer = serializer ?? new DocumentSerializer();
            _messageStorage = messageStorage ?? new DictionaryEnvelopeStorage<Message>();
            _notificationStorage = notificationStorage ?? new DictionaryEnvelopeStorage<Notification>();
            _traceWriter = traceWriter;

            // Pipeline
            _transportBufferBlock = new BufferBlock<ServerHttpTransport>();            
            _transportDictionary = new ConcurrentDictionary<string, ServerHttpTransport>();                        
            _pendingResponsesDictionary = new ConcurrentDictionary<Guid, HttpListenerResponse>();
            _listenerInputBufferBlock = new BufferBlock<HttpListenerContext>();
            _processContextActionBlock = new ActionBlock<HttpListenerContext>(c => ProcessListenerContextAsync(c));
            _listenerInputBufferBlock.LinkTo(_processContextActionBlock);            
            _processTransportOutputActionBlock = new ActionBlock<Envelope>(e => ProcessTransportOutputAsync(e));

            // Context processors
            var sendCommandContextProcessor = new SendCommandContextProcessor(_pendingResponsesDictionary, _traceWriter);
            var sendMessageContextProcessor = new SendMessageContextProcessor(_pendingResponsesDictionary, _traceWriter);
            var getMessagesContextProcessor = new GetMessagesContextProcessor(_messageStorage);            
            var getMessageByIdContextProcessor = new GetMessageByIdContextProcessor(_messageStorage);
            var deleteMessageByIdContextProcessor = new DeleteMessageByIdContextProcessor(_messageStorage);
            var getNotificationsContextProcessor = new GetNotificationsContextProcessor(_notificationStorage);
            var getNotificationByIdContextProcessor = new GetNotificationByIdContextProcessor(_notificationStorage);
            var deleteNotificationByIdContextProcessor = new DeleteNotificationByIdContextProcessor(_notificationStorage);
            _uriTemplateTable = new UriTemplateTable(baseUri);
            _uriTemplateTable.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(sendCommandContextProcessor.Template, sendCommandContextProcessor));
            _uriTemplateTable.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(sendMessageContextProcessor.Template, sendMessageContextProcessor));
            _uriTemplateTable.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(getMessagesContextProcessor.Template, getMessagesContextProcessor));
            _uriTemplateTable.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(getMessageByIdContextProcessor.Template, getMessageByIdContextProcessor));
            _uriTemplateTable.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(deleteMessageByIdContextProcessor.Template, deleteMessageByIdContextProcessor));
            _uriTemplateTable.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(getNotificationsContextProcessor.Template, getNotificationsContextProcessor));
            _uriTemplateTable.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(getNotificationByIdContextProcessor.Template, getNotificationByIdContextProcessor));
            _uriTemplateTable.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(deleteNotificationByIdContextProcessor.Template, deleteNotificationByIdContextProcessor));
            _uriTemplateTable.MakeReadOnly(true);                     

        }

        #endregion

        #region ITransportListener Members

        public Uri[] ListenerUris { get; private set; }

        public Task StartAsync()
        {
            if (_listenerTask != null)
            {
                throw new InvalidOperationException("The listener is already active");
            }

            _httpListener = new HttpListener();
            _httpListener.AuthenticationSchemes = AuthenticationSchemes.Basic;

            foreach (var prefix in _prefixes)
            {
                _httpListener.Prefixes.Add(_basePath + prefix);
            }

            _httpListener.Start();
            _listenerCancellationTokenSource = new CancellationTokenSource();
            _listenerTask = ListenAsync();

            return Task.FromResult<object>(null);
        }

        public async Task<ITransport> AcceptTransportAsync(CancellationToken cancellationToken)
        {
            if (_listenerTask == null)
            {
                throw new InvalidOperationException("The listener was not started.");
            }

            var linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _listenerCancellationTokenSource.Token);

            var transport = await _transportBufferBlock.ReceiveAsync(linkedCancellationToken.Token).ConfigureAwait(false);
            var link = transport.OutputBuffer.LinkTo(_processTransportOutputActionBlock);
            transport.Closed += (sender, e) => link.Dispose();                        
            return transport;
        }

        public async Task StopAsync()
        {
            if (_listenerTask == null)
            {
                throw new InvalidOperationException("The listener was not started.");
            }

            _listenerCancellationTokenSource.Cancel();
            await _listenerTask.ConfigureAwait(false);
            _listenerTask = null;

            _httpListener.Stop();            
            _httpListener = null;
        }

        #endregion

        #region Private Fields

        /// <summary>
        /// Consumes the http listener.
        /// </summary>
        /// <returns></returns>
        private async Task ListenAsync()
        {
            try
            {
                while (!_listenerCancellationTokenSource.IsCancellationRequested)
                {
                    var context = await _httpListener
                        .GetContextAsync()
                        .WithCancellation(_listenerCancellationTokenSource.Token)
                        .ConfigureAwait(false);

                    if (!await _listenerInputBufferBlock.SendAsync(context, _listenerCancellationTokenSource.Token).ConfigureAwait(false))
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        /// <summary>
        /// Process a request received by 
        /// the HTTP listener.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task ProcessListenerContextAsync(HttpListenerContext context)
        {
            Exception exception = null;

            try
            {
                var match = _uriTemplateTable
                    .Match(context.Request.Url)
                    .Where(m => ((IContextProcessor)m.Data).Methods.Contains(context.Request.HttpMethod))
                    .FirstOrDefault();

                if (match != null)
                {
                    bool timedOut = false;

                    var cancellationToken = _requestTimeout.ToCancellationToken();
                    var transport = GetTransport(context);

                    try
                    {
                        var session = await transport.GetSessionAsync(cancellationToken).ConfigureAwait(false);
                        context.Response.Headers.Add(Constants.SESSION_ID_HEADER, session.Id.ToString());

                        if (session.State == SessionState.Established)
                        {
                            var processor = (IContextProcessor)match.Data;
                            await processor.ProcessAsync(context, transport, match, cancellationToken).ConfigureAwait(false);
                        }
                        else if (session.Reason != null)
                        {
                            context.Response.SendResponse(session.Reason);
                        }
                        else
                        {
                            context.Response.SendResponse(HttpStatusCode.NotFound);
                        }
                    }
                    catch (LimeException ex)
                    {
                        context.Response.SendResponse(ex.Reason);                       
                    }
                    catch (OperationCanceledException)
                    {
                        timedOut = true;
                    }

                    if (timedOut)
                    {
                        await transport.CloseAsync(_listenerCancellationTokenSource.Token).ConfigureAwait(false);
                        context.Response.SendResponse(HttpStatusCode.RequestTimeout);
                    }
                }
                else
                {
                    context.Response.SendResponse(HttpStatusCode.NotFound);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception != null)
            {
                if (_writeExceptionsToOutput)
                {
                    await context.Response.SendResponseAsync(
                        HttpStatusCode.InternalServerError, 
                        Constants.TEXT_PLAIN_HEADER_VALUE, 
                        exception.ToString()).ConfigureAwait(false); 
                }
                else
                {
                    if (_traceWriter != null &&
                        _traceWriter.IsEnabled)
                    {
                        await _traceWriter.TraceAsync(exception.ToString(), DataOperation.Send).ConfigureAwait(false);
                    }

                    context.Response.SendResponse(HttpStatusCode.InternalServerError);                    
                }
            }
        }

        /// <summary>
        /// Gets the transport instance
        /// for the specified context
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private ServerHttpTransport GetTransport(HttpListenerContext context)
        {
            var identity = (HttpListenerBasicIdentity)context.User.Identity;
            var transportKey = GetTransportKey(identity);

            var transport = _transportDictionary.GetOrAdd(
                transportKey,
                k =>
                {
                    var newTransport = CreateTransport(identity);
                    newTransport.Closing += (sender, e) =>
                    {
                        _transportDictionary.TryRemove(k, out newTransport);
                    };
                    return newTransport;
                });
            return transport;
        }

        /// <summary>
        /// Creates a new instance
        /// of tranport for the
        /// specified identity
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        private ServerHttpTransport CreateTransport(HttpListenerBasicIdentity identity)
        {
            var transport = new ServerHttpTransport(identity, _useHttps);
            _transportBufferBlock.Post(transport);
            return transport;
        }

        /// <summary>
        /// Gets a hashed key based on
        /// the identity and password.
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        private static string GetTransportKey(HttpListenerBasicIdentity identity)
        {
            return string.Format("{0}:{1}", identity.Name, identity.Password).ToSHA1HashString();
        }

        /// <summary>
        /// Consumes the transports outputs.
        /// </summary>
        /// <param name="envelope"></param>
        /// <returns></returns>
        private async Task ProcessTransportOutputAsync(Envelope envelope)
        {
            Exception exception = null;

            try
            {
                HttpListenerResponse response;

                if (envelope is Message)
                {
                    await _messageStorage.StoreEnvelopeAsync(envelope.To.ToIdentity(), (Message)envelope).ConfigureAwait(false);
                }
                else if (_pendingResponsesDictionary.TryRemove(envelope.Id, out response))
                {
                    if (envelope is Notification)
                    {
                        ProcessNotificationResult((Notification)envelope, response);
                    }
                    else if (envelope is Command)
                    {
                        await ProcessCommandResultAsync((Command)envelope, response).ConfigureAwait(false);                        
                    }
                    else
                    {                        
                        // Put the response back (maybe is an id colision)
                        _pendingResponsesDictionary.TryAdd(envelope.Id, response);                                                
                    }                    
                }                
                else if (envelope is Notification)
                {
                    var notification = (Notification)envelope;
                    var owner = envelope.To.ToIdentity();

                    var existingNotification = await _notificationStorage.GetEnvelopeAsync(owner, envelope.Id).ConfigureAwait(false);
                    if (existingNotification != null)
                    {
                        if (notification.Event == Event.Failed || 
                            existingNotification.Event < notification.Event)
                        {
                            bool updated = false;
                            int tryCount = 0;

                            while (!updated && tryCount++ < 3)
                            {
                                await _notificationStorage.DeleteEnvelopeAsync(owner, envelope.Id).ConfigureAwait(false);                      
                                updated = await _notificationStorage.StoreEnvelopeAsync(owner, notification).ConfigureAwait(false);
                            }
                        }
                    }
                    else
                    {
                        await _notificationStorage.StoreEnvelopeAsync(owner, notification).ConfigureAwait(false);
                    }
                }
                else if (envelope is Command)
                {                    
                    // Register the error, but do not throw an exception
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception != null)
            {
                if (_traceWriter != null &&
                    _traceWriter.IsEnabled)
                {
                    await _traceWriter.TraceAsync("Transport output: " + exception.ToString(), DataOperation.Send).ConfigureAwait(false);
                }
            }
        }  

        private void ProcessNotificationResult(Notification notification, HttpListenerResponse response)
        {            
            if (notification.Event != Event.Failed)
            {
                response.SendResponse(HttpStatusCode.Created);
            }
            else if (notification.Reason != null)
            {
                response.SendResponse(notification.Reason);
            }
            else
            {
                response.SendResponse(HttpStatusCode.InternalServerError);
            }                                
        }

        private async Task ProcessCommandResultAsync(Command command, HttpListenerResponse response)
        {
            if (command.Status == CommandStatus.Success)
            {
                if (command.Resource != null)
                {
                    var contentType = command.Resource.GetMediaType().ToString();
                    var body = _serializer.Serialize(command.Resource);

                    if (_traceWriter != null &&
                        _traceWriter.IsEnabled)
                    {
                        await _traceWriter.TraceAsync(body, DataOperation.Send).ConfigureAwait(false);
                    }

                    await response.SendResponseAsync(
                        HttpStatusCode.OK,
                        contentType,
                        body).ConfigureAwait(false);
                }
                else
                {
                    response.SendResponse(HttpStatusCode.Created);
                }
                response.Close();
            }
            else if (command.Reason != null)
            {
                response.SendResponse(command.Reason);
            }
            else
            {
                response.SendResponse(HttpStatusCode.InternalServerError);
            }            
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
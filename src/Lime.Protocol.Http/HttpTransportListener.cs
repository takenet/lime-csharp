using Lime.Protocol.Http.Serialization;
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
    /*
    # Receive from the channel (long polling)
    GET /messages/

    # Stored messages
    GET /storage/messages/

    # Send to the channel, fire-and-forget
    POST /messages/

    # Send to the channel, with notification
    POST /messages/?id=a9173c7d-038c-4101-b547-939c25d8053e

    # Commands only to the channel (not stored)
    GET /commands/presence/
    POST /commands/presence/
    DELETE /commands/presence/

    # Receive from the channel
    GET /storage/notifications/

    # Send to the channel
    POST /notifications/?id=a9173c7d-038c-4101-b547-939c25d8053e
    */


    /// <summary>
    /// Implements a HTTP listener server 
    /// that supports an emulation layer 
    /// for the LIME protocol.
    /// </summary>
    public class HttpTransportListener : ITransportListener
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
        private readonly ConcurrentDictionary<Guid, HttpListenerContext> _pendingContextsDictionary;
        private readonly ActionBlock<Envelope> _processTransportOutputActionBlock;


        private HttpListener _httpListener;
        private Task _listenerTask;
        private CancellationTokenSource _listenerCancellationTokenSource;
        private readonly string _basePath;
        private readonly string[] _prefixes;


        #endregion

        #region Constants

        private const string ROOT = "/";
        private const string MESSAGES_PATH = "messages";
        private const string COMMANDS_PATH = "commands";
        private const string NOTIFICATIONS_PATH = "notifications";
        private const string HTTP_METHOD_GET = "GET";
        private const string HTTP_METHOD_POST = "POST";
        private const string HTTP_METHOD_DELETE = "DELETE";
        private const string CONTENT_TYPE_HEADER = "Content-Type";
        private const string ENVELOPE_ID_HEADER = "X-Id";
        private const string ENVELOPE_ID_QUERY = "id";
        private const string ENVELOPE_FROM_HEADER = "X-From";
        private const string ENVELOPE_FROM_QUERY = "from";
        private const string ENVELOPE_TO_HEADER = "X-To";
        private const string ENVELOPE_TO_QUERY = "to";
        private const string ENVELOPE_PP_HEADER = "X-Pp";
        private const string ENVELOPE_PP_QUERY = "pp";
        private const string ASYNC_QUERY = "async"; 

        #endregion

        #region Constructor

        public HttpTransportListener(int port, string hostName = "*", bool useHttps = false, bool writeExceptionsToOutput = true)
        {
            if (!HttpListener.IsSupported)
            {
                throw new NotSupportedException("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
            }            

            var scheme = Uri.UriSchemeHttps;
            _useHttps = useHttps;
            if (!useHttps)
            {
                scheme = Uri.UriSchemeHttp;
            }

            _writeExceptionsToOutput = writeExceptionsToOutput;

            _basePath = string.Format("{0}://{1}:{2}", scheme, hostName, port);
            _prefixes = new string[]
            {
                ROOT + MESSAGES_PATH + ROOT,
                ROOT + COMMANDS_PATH + ROOT,
                ROOT + NOTIFICATIONS_PATH + ROOT
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

            _requestTimeout = TimeSpan.FromSeconds(60);
            _serializer = new DocumentSerializer();

            _transportBufferBlock = new BufferBlock<ServerHttpTransport>();            
            _transportDictionary = new ConcurrentDictionary<string, ServerHttpTransport>();
            
            // Pipelines
            _pendingContextsDictionary = new ConcurrentDictionary<Guid, HttpListenerContext>();
            _listenerInputBufferBlock = new BufferBlock<HttpListenerContext>();
            _processContextActionBlock = new ActionBlock<HttpListenerContext>(c => ProcessListenerRequestAsync(c));
            _listenerInputBufferBlock.LinkTo(_processContextActionBlock);            
            _processTransportOutputActionBlock = new ActionBlock<Envelope>(e => ProcessTransportOutputAsync(e));            
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
        /// Process a request enqueued by
        /// the HTTP listener.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task ProcessListenerRequestAsync(HttpListenerContext context)
        {
            var cancellationToken = _requestTimeout.ToCancellationToken();
            var transport = GetTransport(context);

            Exception exception = null;

            try
            {
                var session = await transport.GetSessionAsync(cancellationToken).ConfigureAwait(false);
                if (session.State == SessionState.Established)
                {
                    var method = context.Request.HttpMethod;
                    var path = context.Request.Url.GetRootPath();
                    switch (path)
                    {
                        case MESSAGES_PATH:
                            if (method.Equals(HTTP_METHOD_POST))
                            {                                                                                                
                                // Put the message to the transport
                                var message = await GetMessageAsync(context.Request).ConfigureAwait(false);

                                bool isAsync = message.Id == Guid.Empty;
                                if (!isAsync)
                                {
                                    bool.TryParse(context.Request.QueryString.Get(ASYNC_QUERY), out isAsync);
                                }

                                await ProcessEnvelopeAsync(message, transport, context, isAsync, cancellationToken).ConfigureAwait(false);
                            }
                            else if (method.Equals(HTTP_METHOD_GET))
                            {
                                // TODO: Get messages from storage
                            }
                            else
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                                context.Response.Close();
                            }
                            break;

                        case COMMANDS_PATH:
                            var command = await GetCommandAsync(context.Request).ConfigureAwait(false);
                            await ProcessEnvelopeAsync(command, transport, context, false, cancellationToken).ConfigureAwait(false);
                            break;

                        case NOTIFICATIONS_PATH:
                            if (method.Equals(HTTP_METHOD_POST))
                            {
                                // TODO: Send notification
                            }
                            else if (method.Equals(HTTP_METHOD_GET))
                            {
                                // TODO: Get messages from storage
                            }
                            else
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                                context.Response.Close();
                            }
                            break;

                        default:
                            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            context.Response.Close();
                            break;
                    }
                }
                else if (session.Reason != null)
                {
                    context.Response.StatusCode = (int)GetHttpStatusCode(session.Reason.Code);
                    context.Response.StatusDescription = session.Reason.Description;
                    context.Response.Close();
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Response.Close();
                }                

            }
            catch (LimeException ex)
            {
                context.Response.StatusCode = (int)GetHttpStatusCode(ex.Reason.Code);
                context.Response.StatusDescription = ex.Reason.Description;
                context.Response.Close();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception != null)
            {
                if (exception is OperationCanceledException)
                {
                    await transport.CloseAsync(_listenerCancellationTokenSource.Token).ConfigureAwait(false);
                    context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    context.Response.Close();
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    if (_writeExceptionsToOutput)
                    {
                        using (var writer = new StreamWriter(context.Response.OutputStream))
                        {
                            await writer.WriteAsync(exception.ToString()).ConfigureAwait(false);
                        }
                    }

                    context.Response.Close();
                }
            }
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
                HttpListenerContext context;

                if (envelope is Message)
                {
                    // TODO: Stores the envelope
                }
                else if (_pendingContextsDictionary.TryRemove(envelope.Id, out context))
                {
                    if (envelope is Notification)
                    {
                        var notification = (Notification)envelope;
                        ProcessNotificationResult(context.Response, notification);
                    }
                    else if (envelope is Command)
                    {
                        var command = (Command)envelope;
                        await ProcessCommandResultAsync(context.Response, command).ConfigureAwait(false);
                    }
                    else
                    {
                        // Message and sessions should not be here...
                        context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    }

                    context.Response.Close();
                }                
                else if (envelope is Notification)
                {
                    // TODO: Stores the envelope
                }
                else
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

            }
        }

        /// <summary>
        /// Process the envelope request.
        /// </summary>
        /// <param name="envelope"></param>
        /// <param name="transport"></param>
        /// <param name="context"></param>
        /// <param name="releaseContext">if true, the context should be closed; otherwise, it will be added to the pending list and waits for a transport result.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ProcessEnvelopeAsync(Envelope envelope, ServerHttpTransport transport, HttpListenerContext context, bool isAsync, CancellationToken cancellationToken)
        {
            if (isAsync)
            {
                await transport.InputBuffer.SendAsync(envelope, cancellationToken).ConfigureAwait(false);
                context.Response.StatusCode = (int)HttpStatusCode.Accepted;
                context.Response.Close();
            }
            else
            {
                // Register the context for callback
                if (_pendingContextsDictionary.TryAdd(envelope.Id, context))
                {
                    // The cancellationToken can be collected by the GC before this?
                    cancellationToken.Register(() =>
                    {
                        HttpListenerContext c;
                        if (_pendingContextsDictionary.TryRemove(envelope.Id, out c))
                        {
                            c.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                            c.Response.Close();
                        };
                    });

                    await transport.InputBuffer.SendAsync(envelope, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    context.Response.Close();
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

        private async Task<Message> GetMessageAsync(HttpListenerRequest request)
        {
            Message message = null;

            var content = await GetDocumentAsync(request).ConfigureAwait(false);
            if (content != null)
            {
                message = new Message()
                {
                    Content = content
                };
                FillEnvelopeFromRequest(message, request);
            }
            else
            {
                throw new LimeException(ReasonCodes.VALIDATION_EMPTY_DOCUMENT, "Invalid or empty content");
            }

            return message;
        }

        private async Task<Command> GetCommandAsync(HttpListenerRequest request)
        {
            Command command = null;            

            CommandMethod method;
            if (TryConvertToCommandMethod(request.HttpMethod, out method))
            {                               
                var limeUriFragment = request.Url.Segments.Except(new[] { COMMANDS_PATH + ROOT }).Aggregate((s1, s2) => s1 + s2);
                if (!string.IsNullOrWhiteSpace(limeUriFragment))
                {
                    command = new Command();
                    FillEnvelopeFromRequest(command, request);

                    if (command.Id != Guid.Empty)
                    {
                        command.Method = method;
                        command.Uri = new LimeUri(limeUriFragment);

                        if (command.Method == CommandMethod.Set ||
                            command.Method == CommandMethod.Observe)
                        {
                            command.Resource = await GetDocumentAsync(request).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        throw new LimeException(ReasonCodes.VALIDATION_ERROR, "Invalid or empty id");
                    }
                }
                else
                {
                    throw new LimeException(ReasonCodes.VALIDATION_INVALID_URI, "Invalid command URI");
                }
            }
            else
            {
                throw new LimeException(ReasonCodes.VALIDATION_INVALID_METHOD, "Invalid method");
            }

            return command;
        }        

        private bool TryConvertToCommandMethod(string httpMethod, out CommandMethod commandMethod)
        {
            switch (httpMethod)
            {
                case HTTP_METHOD_GET:
                    commandMethod = CommandMethod.Get;
                    return true;
                case HTTP_METHOD_POST:
                    commandMethod = CommandMethod.Set;
                    return true;
                case HTTP_METHOD_DELETE:
                    commandMethod = CommandMethod.Delete;
                    return true;
                default:
                    commandMethod = default(CommandMethod);
                    return false;
            }
        }

        private async Task<Document> GetDocumentAsync(HttpListenerRequest request)
        {
            Document document = null;

            MediaType mediaType = null;
            MediaType.TryParse(request.Headers.Get(CONTENT_TYPE_HEADER), out mediaType);

            if (mediaType != null)
            {                
                using (var streamReader = new StreamReader(request.InputStream))
                {
                    var body = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                    document = _serializer.Deserialize(body, mediaType);
                }
            }

            return document;        
        }

        private void FillEnvelopeFromRequest(Envelope envelope, HttpListenerRequest request)
        {
            if (envelope != null)
            {
                Guid id;
                Guid.TryParse(request.GetValue(ENVELOPE_ID_HEADER, ENVELOPE_ID_QUERY), out id);
                Node from;
                Node.TryParse(request.GetValue(ENVELOPE_FROM_HEADER, ENVELOPE_FROM_QUERY), out from);
                Node to;
                Node.TryParse(request.GetValue(ENVELOPE_TO_HEADER, ENVELOPE_TO_QUERY), out to);
                Node pp;
                Node.TryParse(request.GetValue(ENVELOPE_PP_HEADER, ENVELOPE_PP_QUERY), out pp);

                envelope.Id = id;
                envelope.From = from;
                envelope.To = to;
                envelope.Pp = pp;
            }
        }

        private HttpStatusCode GetHttpStatusCode(int reasonCode)
        {            
            if (reasonCode >= 20 && reasonCode < 30)
            {
                // Validation errors
                return HttpStatusCode.BadRequest;
            }
            else if ((reasonCode >= 10 && reasonCode < 20) || (reasonCode >= 30 && reasonCode < 40))
            {
                // Session or Authorization errors
                return HttpStatusCode.Unauthorized;
            }            
            
            return HttpStatusCode.Forbidden;
        }

        private void ProcessNotificationResult(HttpListenerResponse response, Notification notification)
        {
            if (notification.Event == Event.Dispatched)
            {
                response.StatusCode = (int)HttpStatusCode.Created;
            }
            else if (notification.Event == Event.Failed)
            {
                response.StatusCode = (int)GetHttpStatusCode(notification.Reason.Code);
                response.StatusDescription = notification.Reason.Description;
            }
        }


        private async Task ProcessCommandResultAsync(HttpListenerResponse response, Command command)
        {
            if (command.Status == CommandStatus.Success)
            {
                response.StatusCode = (int)HttpStatusCode.Created;
            }
            else
            {
                response.StatusCode = (int)GetHttpStatusCode(command.Reason.Code);
                response.StatusDescription = command.Reason.Description;
            }

            if (command.Resource != null)
            {
                var mediaType = command.Resource.GetMediaType();
                response.Headers.Add(CONTENT_TYPE_HEADER, mediaType.ToString());

                using (var writer = new StreamWriter(response.OutputStream))
                {
                    var documentString = _serializer.Serialize(command.Resource);
                    await writer.WriteAsync(documentString).ConfigureAwait(false);
                }
            }
        }

        #endregion
    }
}
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


        private const string ROOT = "/";
        private const string MESSAGES_PATH = "messages";
        private const string COMMANDS_PATH = "commands";
        private const string NOTIFICATIONS_PATH = "notifications";
        private const string HTTP_METHOD_GET = "GET";
        private const string HTTP_METHOD_POST = "POST";
        private const string ASYNC_KEY = "async";
        private const string CONTENT_TYPE_HEADER = "Content-Type";
        private const string ENVELOPE_ID_HEADER = "X-Id";
        private const string ENVELOPE_FROM_HEADER = "X-From";
        private const string ENVELOPE_TO_HEADER = "X-To";
        private const string ENVELOPE_PP_HEADER = "X-Pp";

        #endregion

        #region Constructor

        public HttpTransportListener(int port, string hostName = "*", bool useHttps = false)
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

        private async Task ProcessListenerRequestAsync(HttpListenerContext context)
        {
            var cancellationToken = _requestTimeout.ToCancellationToken();
            var transport = GetTransport(context);

            var timedOut = false;

            try
            {
                var session = await transport.GetSessionAsync(cancellationToken).ConfigureAwait(false);
                if (session.State == SessionState.Established)
                {
                    if (context.Request.Url.Segments.Length >= 2)
                    {
                        var path = context.Request.Url.Segments[1].TrimEnd('/').ToLowerInvariant();
                        switch (path)
                        {
                            case MESSAGES_PATH:

                                if (context.Request.HttpMethod.Equals(HTTP_METHOD_POST))
                                {
                                    var message = await GetMessageAsync(context.Request).ConfigureAwait(false);
                                    if (message != null)
                                    {
                                        bool isAsync;
                                        bool.TryParse(context.Request.QueryString.Get(ASYNC_KEY), out isAsync);
                                        isAsync = isAsync && message.Id != Guid.Empty;
                                        if (!isAsync)
                                        {
                                            // Register the context for callback
                                            if (_pendingContextsDictionary.TryAdd(message.Id, context))
                                            {
                                                // The cancellationToken can be collected by the GC before that?
                                                cancellationToken.Register(() =>
                                                {
                                                    HttpListenerContext c;
                                                    if (_pendingContextsDictionary.TryRemove(message.Id, out c))
                                                    {
                                                        c.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                                                        c.Response.Close();
                                                    };
                                                });
                                            }
                                            else
                                            {
                                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                                context.Response.Close();
                                                return;
                                            }
                                        }

                                        await transport.InputBuffer.SendAsync(message, cancellationToken).ConfigureAwait(false);

                                        if (isAsync)
                                        {
                                            context.Response.StatusCode = (int)HttpStatusCode.Accepted;
                                            context.Response.Close();
                                        }
                                    }
                                    else
                                    {
                                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                        context.Response.Close();
                                    }
                                }
                                else if (context.Request.HttpMethod.Equals(HTTP_METHOD_GET))
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
                                break;

                            case NOTIFICATIONS_PATH:
                                break;

                            default:
                                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                                context.Response.Close();
                                break;
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        context.Response.Close();
                    }
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    context.Response.Close();
                }
            }
            catch (OperationCanceledException)
            {
                timedOut = true;
            }            

            if (timedOut)
            {
                await transport.CloseAsync(_listenerCancellationTokenSource.Token).ConfigureAwait(false);
                context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                context.Response.Close();
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
            return string.Format("{0}_{1}", identity.Name, identity.Password).ToSHA1HashString();
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

            return message;
        }

        private async Task<Command> GetCommandAsync(HttpListenerRequest request)
        {
            Command command = null;

            // TODO: Get LIME Uri

            // TODO: Get method



            var resource = await GetDocumentAsync(request).ConfigureAwait(false);

            

            if (command != null)
            {
                FillEnvelopeFromRequest(command, request);
            }

            return command;
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
                Guid.TryParse(request.Headers.Get(ENVELOPE_ID_HEADER), out id);
                Node from;
                Node.TryParse(request.Headers.Get(ENVELOPE_FROM_HEADER), out from);
                Node to;
                Node.TryParse(request.Headers.Get(ENVELOPE_TO_HEADER), out to);
                Node pp;
                Node.TryParse(request.Headers.Get(ENVELOPE_PP_HEADER), out pp);

                envelope.Id = id;
                envelope.From = from;
                envelope.To = to;
                envelope.Pp = pp;
            }
        }

        /// <summary>
        /// Gets an envelope from the HTTP request.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<Envelope> GetEnvelopeAsync(HttpListenerRequest request)
        {                       
            MediaType mediaType = null;
            MediaType.TryParse(request.Headers.Get(CONTENT_TYPE_HEADER), out mediaType);

            Envelope envelope = null;

            if (request.Url.Segments.Length >= 2)
            {
                var path = request.Url.Segments[1].TrimEnd('/');
                if (path.Equals(MESSAGES_PATH, StringComparison.OrdinalIgnoreCase) ||
                    path.Equals(COMMANDS_PATH, StringComparison.OrdinalIgnoreCase))
                {
                    if (mediaType != null)
                    {
                        Document document;
                        
                        using (var streamReader = new StreamReader(request.InputStream))
                        {
                            var body = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                            document = _serializer.Deserialize(body, mediaType);
                        }


                        if (document != null)
                        {
                            if (path.Equals(COMMANDS_PATH, StringComparison.OrdinalIgnoreCase))
                            {
                                envelope = new Command();
                            }
                            else
                            {
                                envelope = new Message()
                                {
                                    Content = document
                                };
                            }

                            
                        }
                    }                    
                }
                else if (path.Equals(COMMANDS_PATH, StringComparison.OrdinalIgnoreCase))
                {                    
                    if (mediaType != null)
                    {
                        var command = new Command();

                    }
                }
                else if (path.Equals(NOTIFICATIONS_PATH, StringComparison.OrdinalIgnoreCase))
                {

                }
            }

            if (envelope != null)
            {
                Guid id;
                Guid.TryParse(request.Headers.Get(ENVELOPE_ID_HEADER), out id);                
                Node from;
                Node.TryParse(request.Headers.Get(ENVELOPE_FROM_HEADER), out from);                
                Node to;
                Node.TryParse(request.Headers.Get(ENVELOPE_TO_HEADER), out to);
                Node pp;
                Node.TryParse(request.Headers.Get(ENVELOPE_PP_HEADER), out pp);

                envelope.Id = id;
                envelope.From = from;
                envelope.To = to;
                envelope.Pp = pp;                     
            }

            return envelope;
        }


        /// <summary>
        /// Consumes the transports outputs.
        /// </summary>
        /// <param name="envelope"></param>
        /// <returns></returns>
        private async Task ProcessTransportOutputAsync(Envelope envelope)
        {
            // Check for a pending request 
            HttpListenerContext context;
            if (_pendingContextsDictionary.TryRemove(envelope.Id, out context))
            {                                
                if (envelope is Notification)
                {
                    var notification = (Notification)envelope;
                    if (notification.Event == Event.Dispatched)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Created;
                    }
                    else if (notification.Event == Event.Failed)
                    {
                        context.Response.StatusCode = (int)GetHttpStatusCode(notification.Reason.Code);
                        context.Response.StatusDescription = notification.Reason.Description;
                    }
                }
                else if (envelope is Command)
                {
                    var command = (Command)envelope;
                    if (command.Status == CommandStatus.Success)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Created;
                    }
                    else
                    {
                        context.Response.StatusCode = (int)GetHttpStatusCode(command.Reason.Code);
                        context.Response.StatusDescription = command.Reason.Description;
                    }

                    if (command.Resource != null)
                    {
                        var mediaType = command.Resource.GetMediaType();
                        context.Response.Headers.Add(CONTENT_TYPE_HEADER, mediaType.ToString());

                        using (var writer = new StreamWriter(context.Response.OutputStream))
                        {
                            var documentString =_serializer.Serialize(command.Resource);
                            await writer.WriteAsync(documentString).ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    // Message and sessions should not be here...
                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                }

                context.Response.Close();
            }
            else
            {
                // TODO: Stores the envelope
            }
        }

        private HttpStatusCode GetHttpStatusCode(int reasonCode)
        {
            return HttpStatusCode.Gone;
        }

        #endregion
    }
}
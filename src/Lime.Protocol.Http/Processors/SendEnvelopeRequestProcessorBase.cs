using Lime.Protocol.Http.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Lime.Protocol.Http.Processors
{
    public abstract class SendEnvelopeRequestProcessorBase<T> : IRequestProcessor 
        where T : Envelope
    {

        private readonly IDocumentSerializer _serializer;
        private readonly ConcurrentDictionary<Guid, HttpListenerResponse> _pendingResponsesDictionary;


        #region Constructor

        public SendEnvelopeRequestProcessorBase(HashSet<string> methods, UriTemplate template, IDocumentSerializer serializer, ConcurrentDictionary<Guid, HttpListenerResponse> pendingResponsesDictionary)
        {
            Methods = methods;
            Template = template;

            _serializer = serializer;
            _pendingResponsesDictionary = pendingResponsesDictionary;

        }

        #endregion

        #region IRequestProcessor Members

        public HashSet<string> Methods { get; private set; }


        public UriTemplate Template { get; private set; }

        public virtual async Task ProcessAsync(HttpListenerContext context, ServerHttpTransport transport, UriTemplateMatch match, CancellationToken cancellationToken)
        {
            var envelope = await ParseEnvelopeAsync(context.Request).ConfigureAwait(false);

            bool isAsync = envelope is Message;
            if (isAsync)
            {
                bool.TryParse(context.Request.QueryString.Get(Constants.ASYNC_QUERY), out isAsync);
            }

            await ProcessEnvelopeAsync(envelope, transport, context.Response, isAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion
        
        #region Protected Methods

        protected abstract Task<T> ParseEnvelopeAsync(HttpListenerRequest request);

        /// <summary>
        /// Process the envelope request.
        /// </summary>
        /// <param name="envelope"></param>
        /// <param name="transport"></param>
        /// <param name="contextresponse"></param>
        /// <param name="isAsync"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async Task ProcessEnvelopeAsync(Envelope envelope, ServerHttpTransport transport, HttpListenerResponse response, bool isAsync, CancellationToken cancellationToken)
        {
            response.Headers.Add(Constants.ENVELOPE_ID_HEADER, envelope.Id.ToString());

            if (isAsync)
            {
                await transport.InputBuffer.SendAsync(envelope, cancellationToken).ConfigureAwait(false);
                response.StatusCode = (int)HttpStatusCode.Accepted;
                response.Close();
            }
            else
            {
                // Register the context for callback
                if (_pendingResponsesDictionary.TryAdd(envelope.Id, response))
                {
                    // The cancellationToken can be collected by the GC before this?
                    cancellationToken.Register(() =>
                    {
                        HttpListenerResponse r;
                        if (_pendingResponsesDictionary.TryRemove(envelope.Id, out r))
                        {
                            r.StatusCode = (int)HttpStatusCode.RequestTimeout;
                            r.Close();
                        };
                    });

                    await transport.InputBuffer.SendAsync(envelope, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.Conflict;
                    response.Close();
                }
            }
        }


        protected async Task<Document> ParseDocumentAsync(HttpListenerRequest request)
        {
            Document document = null;

            MediaType mediaType;
            if (MediaType.TryParse(request.ContentType, out mediaType))
            {
                using (var streamReader = new StreamReader(request.InputStream))
                {
                    var body = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                    document = _serializer.Deserialize(body, mediaType);
                }
            }

            return document;
        }


        protected void FillEnvelopeFromRequest(Envelope envelope, HttpListenerRequest request)
        {
            if (envelope != null)
            {
                Guid id;
                if (!Guid.TryParse(request.GetValue(Constants.ENVELOPE_ID_HEADER, Constants.ENVELOPE_ID_QUERY), out id))
                {
                    id = Guid.NewGuid();
                }
                Node from;
                Node.TryParse(request.GetValue(Constants.ENVELOPE_FROM_HEADER, Constants.ENVELOPE_FROM_QUERY), out from);
                Node to;
                Node.TryParse(request.GetValue(Constants.ENVELOPE_TO_HEADER, Constants.ENVELOPE_TO_QUERY), out to);
                Node pp;
                Node.TryParse(request.GetValue(Constants.ENVELOPE_PP_HEADER, Constants.ENVELOPE_PP_QUERY), out pp);

                envelope.Id = id;
                envelope.From = from;
                envelope.To = to;
                envelope.Pp = pp;
            }
        }

        #endregion
    }
}

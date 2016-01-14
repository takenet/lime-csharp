using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Transport.Http.Serialization;

namespace Lime.Transport.Http.Processors
{
    public abstract class SendEnvelopeHttpProcessorBase<T> : IHttpProcessor
        where T : Envelope, new()
    {
        #region Private Fields

        protected readonly IDocumentSerializer _serializer;
        protected readonly ITraceWriter _traceWriter;

        #endregion

        #region Constructor

        public SendEnvelopeHttpProcessorBase(HashSet<string> methods, UriTemplate template, IDocumentSerializer serializer, ITraceWriter traceWriter = null)
        {
            Methods = methods;
            Template = template;

            _serializer = serializer;
            _traceWriter = traceWriter;
        }

        #endregion

        #region IHttpProcessor Members

        public HashSet<string> Methods { get; private set; }


        public UriTemplate Template { get; private set; }


        public virtual async Task<HttpResponse> ProcessAsync(HttpRequest request, UriTemplateMatch match, ITransportSession transport, CancellationToken cancellationToken)
        {
            var envelope = await GetEnvelopeFromRequestAsync(request).ConfigureAwait(false);            
            await transport.SubmitAsync(envelope, cancellationToken).ConfigureAwait(false);
            return new HttpResponse(request.CorrelatorId, HttpStatusCode.Accepted, body: envelope.Id.ToString());                     
        }

        #endregion
        
        #region Protected Methods

        protected async Task<T> GetEnvelopeFromRequestAsync(HttpRequest request)
        {
            var envelope = new T();
                       
            Node from;
            Node.TryParse(request.GetValue(Constants.ENVELOPE_FROM_HEADER, Constants.ENVELOPE_FROM_QUERY), out from);
            Node to;
            Node.TryParse(request.GetValue(Constants.ENVELOPE_TO_HEADER, Constants.ENVELOPE_TO_QUERY), out to);
            Node pp;
            Node.TryParse(request.GetValue(Constants.ENVELOPE_PP_HEADER, Constants.ENVELOPE_PP_QUERY), out pp);

            envelope.Id = request.CorrelatorId;
            envelope.From = from;
            envelope.To = to;
            envelope.Pp = pp;

            await FillEnvelopeAsync(envelope, request).ConfigureAwait(false);

            return envelope;
        }

        protected abstract Task FillEnvelopeAsync(T envelope, HttpRequest request);

        protected async Task<Document> ParseDocumentAsync(HttpRequest request)
        {
            Document document = null;

            MediaType mediaType;
            if (MediaType.TryParse(request.Headers[HttpRequestHeader.ContentType], out mediaType))
            {
                using (var streamReader = new StreamReader(request.BodyStream))
                {
                    var body = await streamReader.ReadToEndAsync().ConfigureAwait(false);

                    if (_traceWriter != null &&
                        _traceWriter.IsEnabled)
                    {
                        await _traceWriter.TraceAsync(body, DataOperation.Receive).ConfigureAwait(false);
                    }

                    document = _serializer.Deserialize(body, mediaType);
                }
            }

            return document;
        }

        #endregion        
    }
}

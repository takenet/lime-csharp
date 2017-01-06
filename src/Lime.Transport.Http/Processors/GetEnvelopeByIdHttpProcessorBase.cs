using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Serialization;
using Lime.Transport.Http.Storage;

namespace Lime.Transport.Http.Processors
{
    public abstract class GetEnvelopeByIdHttpProcessorBase<T> : IHttpProcessor
        where T : Envelope
    {
        private readonly IEnvelopeStorage<T> _envelopeStorage;
        private readonly IDocumentSerializer _serializer;

        public GetEnvelopeByIdHttpProcessorBase(IEnvelopeStorage<T> envelopeStorage, string path)
        {
            _envelopeStorage = envelopeStorage;
            _serializer = new DocumentSerializer();

            Methods = new HashSet<string> { Constants.HTTP_METHOD_GET };
            Template = new UriTemplate(string.Format("/{0}/{{id}}", path));
        }

        public HashSet<string> Methods { get; }

        public UriTemplate Template { get; }

        public async Task<HttpResponse> ProcessAsync(HttpRequest request, UriTemplateMatch match, ITransportSession transport, CancellationToken cancellationToken)
        {
            Identity owner;
            var id = match.BoundVariables.Get("id");

            if (!id.IsNullOrEmpty() &&
                Identity.TryParse(request.User.Identity.Name, out owner))
            {
                var envelope = await _envelopeStorage.GetEnvelopeAsync(owner, id).ConfigureAwait(false);
                if (envelope != null)
                {
                    var response = GetEnvelopeResponse(envelope, request); 
                    if (envelope.From != null)
                    {
                        response.Headers.Add(Constants.ENVELOPE_FROM_HEADER, envelope.From.ToString());
                    }

                    if (envelope.To != null)
                    {
                        response.Headers.Add(Constants.ENVELOPE_TO_HEADER, envelope.To.ToString());
                    }

                    if (envelope.Pp != null)
                    {
                        response.Headers.Add(Constants.ENVELOPE_PP_HEADER, envelope.Pp.ToString());
                    }
                    
                    return response;
                }
                return new HttpResponse(request.CorrelatorId, HttpStatusCode.NotFound);
            }
            return new HttpResponse(request.CorrelatorId, HttpStatusCode.BadRequest);
        }

        protected abstract HttpResponse GetEnvelopeResponse(T envelope, HttpRequest request);
    }
}

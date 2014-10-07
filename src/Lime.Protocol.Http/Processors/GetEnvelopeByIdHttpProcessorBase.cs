using Lime.Protocol.Http.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Http.Serialization;

namespace Lime.Protocol.Http.Processors
{
    public abstract class GetEnvelopeByIdHttpProcessorBase<T> : IHttpProcessor
        where T : Envelope
    {
        private readonly IEnvelopeStorage<T> _envelopeStorage;
        private readonly IDocumentSerializer _serializer;

        #region Constructor

        public GetEnvelopeByIdHttpProcessorBase(IEnvelopeStorage<T> envelopeStorage, string path)
        {
            _envelopeStorage = envelopeStorage;
            _serializer = new DocumentSerializer();

            Methods = new HashSet<string> { Constants.HTTP_METHOD_GET };
            Template = new UriTemplate(string.Format("/{0}/{{id}}", path));
        }

        #endregion

        #region IHttpProcessor Members

        public HashSet<string> Methods { get; private set; }

        public UriTemplate Template { get; private set; }

        public async Task<HttpResponse> ProcessAsync(HttpRequest request, UriTemplateMatch match, ServerHttpTransport transport, CancellationToken cancellationToken)
        {
            Identity owner;
            Guid id;

            if (Identity.TryParse(request.User.Identity.Name, out owner) &&
                Guid.TryParse(match.BoundVariables.Get("id"), out id))
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
                else
                {
                    return new HttpResponse(request.CorrelatorId, HttpStatusCode.NotFound);
                }
            }
            else
            {
                return new HttpResponse(request.CorrelatorId, HttpStatusCode.BadRequest);
            }
        }

        #endregion

        protected abstract HttpResponse GetEnvelopeResponse(T envelope, HttpRequest request);
    }
}

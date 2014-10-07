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
    public class DeleteEnvelopeByIdHttpProcessor<T> : IHttpProcessor
        where T : Envelope
    {
        private readonly IEnvelopeStorage<T> _envelopeStorage;
        private readonly IDocumentSerializer _serializer;

        #region Constructor

        public DeleteEnvelopeByIdHttpProcessor(IEnvelopeStorage<T> envelopeStorage, string path)
        {
            _envelopeStorage = envelopeStorage;
            _serializer = new DocumentSerializer();

            Methods = new HashSet<string> { Constants.HTTP_METHOD_DELETE };
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
                if (await _envelopeStorage.DeleteEnvelopeAsync(owner, id).ConfigureAwait(false))
                {
                    return new HttpResponse(request.CorrelatorId, HttpStatusCode.OK);

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
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Transport.Http.Storage;

namespace Lime.Transport.Http.Processors
{
    public class DeleteEnvelopeByIdHttpProcessor<T> : IHttpProcessor
        where T : Envelope
    {
        private readonly IEnvelopeStorage<T> _envelopeStorage;

        public DeleteEnvelopeByIdHttpProcessor(IEnvelopeStorage<T> envelopeStorage, string path)
        {
            _envelopeStorage = envelopeStorage;
       
            Methods = new HashSet<string> { Constants.HTTP_METHOD_DELETE };
            Template = new UriTemplate(string.Format("/{0}/{{id}}", path));
        }

        public HashSet<string> Methods { get; }

        public UriTemplate Template { get; }

        public async Task<HttpResponse> ProcessAsync(HttpRequest request, UriTemplateMatch match, ITransportSession transport, CancellationToken cancellationToken)
        {            
            if (request == null) throw new ArgumentNullException(nameof(request));            
            if (match == null) throw new ArgumentNullException(nameof(match));
                        
            var id = match.BoundVariables.Get("id");
            Identity owner;
            if (!id.IsNullOrEmpty() &&
                Identity.TryParse(request.User.Identity.Name, out owner))
            {
                if (await _envelopeStorage.DeleteEnvelopeAsync(owner, id).ConfigureAwait(false))
                {
                    return new HttpResponse(request.CorrelatorId, HttpStatusCode.OK);

                }
                return new HttpResponse(request.CorrelatorId, HttpStatusCode.NotFound);
            }
            return new HttpResponse(request.CorrelatorId, HttpStatusCode.BadRequest);
        }
    }
}

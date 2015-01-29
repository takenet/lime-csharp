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
        #region Private Fields

        private readonly IEnvelopeStorage<T> _envelopeStorage;

        #endregion

        #region Constructor

        public DeleteEnvelopeByIdHttpProcessor(IEnvelopeStorage<T> envelopeStorage, string path)
        {
            _envelopeStorage = envelopeStorage;
       
            Methods = new HashSet<string> { Constants.HTTP_METHOD_DELETE };
            Template = new UriTemplate(string.Format("/{0}/{{id}}", path));
        }

        #endregion

        #region IHttpProcessor Members

        public HashSet<string> Methods { get; private set; }

        public UriTemplate Template { get; private set; }

        public async Task<HttpResponse> ProcessAsync(HttpRequest request, UriTemplateMatch match, ITransportSession transport, CancellationToken cancellationToken)
        {            
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

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

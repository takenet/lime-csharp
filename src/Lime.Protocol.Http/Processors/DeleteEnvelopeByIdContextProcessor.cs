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
    public class DeleteEnvelopeByIdContextProcessor<T> : IContextProcessor
        where T : Envelope
    {
        private readonly IEnvelopeStorage<T> _envelopeStorage;
        private readonly IDocumentSerializer _serializer;

        #region Constructor

        public DeleteEnvelopeByIdContextProcessor(IEnvelopeStorage<T> envelopeStorage, string path)
        {
            _envelopeStorage = envelopeStorage;
            _serializer = new DocumentSerializer();

            Methods = new HashSet<string> { Constants.HTTP_METHOD_DELETE };
            Template = new UriTemplate(string.Format("/{0}/{{id}}", path));
        }

        #endregion

        #region IRequestProcessor Members

        public HashSet<string> Methods { get; private set; }

        public UriTemplate Template { get; private set; }

        public async Task ProcessAsync(HttpListenerContext context, ServerHttpTransport transport, UriTemplateMatch match, CancellationToken cancellationToken)
        {
            Identity owner;
            Guid id;

            if (Identity.TryParse(context.User.Identity.Name, out owner) &&
                Guid.TryParse(match.BoundVariables.Get("id"), out id))
            {
                if (await _envelopeStorage.DeleteEnvelopeAsync(owner, id).ConfigureAwait(false))
                {
                    context.Response.SendResponse(HttpStatusCode.OK);
                    
                }
                else
                {
                    context.Response.SendResponse(HttpStatusCode.NotFound);
                }
            }
            else
            {
                context.Response.SendResponse(HttpStatusCode.BadRequest);
            }
        }

        #endregion
    }
}

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
    public abstract class GetEnvelopeByIdContextProcessorBase<T> : IContextProcessor
        where T : Envelope
    {
        private readonly IEnvelopeStorage<T> _envelopeStorage;
        private readonly IDocumentSerializer _serializer;

        #region Constructor

        public GetEnvelopeByIdContextProcessorBase(IEnvelopeStorage<T> envelopeStorage, string path)
        {
            _envelopeStorage = envelopeStorage;
            _serializer = new DocumentSerializer();

            Methods = new HashSet<string> { Constants.HTTP_METHOD_GET };
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
                var envelope = await _envelopeStorage.GetEnvelopeAsync(owner, id).ConfigureAwait(false);
                if (envelope != null)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;

                    if (envelope.From != null)
                    {
                        context.Response.Headers.Add(Constants.ENVELOPE_FROM_HEADER, envelope.From.ToString());
                    }
                    
                    if (envelope.To != null)
                    {
                        context.Response.Headers.Add(Constants.ENVELOPE_TO_HEADER, envelope.To.ToString());
                    }

                    if (envelope.Pp != null)
                    {
                        context.Response.Headers.Add(Constants.ENVELOPE_PP_HEADER, envelope.Pp.ToString());
                    }

                    await WriteEnvelopeResultAsync(envelope, context.Response).ConfigureAwait(false);
                    context.Response.Close();
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


        protected abstract Task WriteEnvelopeResultAsync(T envelope, HttpListenerResponse response);

    }
}

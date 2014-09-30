using Lime.Protocol.Http.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Http.Processors
{
    public class GetEnvelopesContextProcessor<T> : IContextProcessor 
        where T : Envelope
    {
        private readonly IEnvelopeStorage<T> _envelopeStorage;

        #region Constructor

        public GetEnvelopesContextProcessor(IEnvelopeStorage<T> envelopeStorage, string path)
        {
            _envelopeStorage = envelopeStorage;

            Methods = new HashSet<string> { Constants.HTTP_METHOD_GET };
            Template = new UriTemplate(string.Format("/{0}", path));
        }

        #endregion

        #region IRequestProcessor Members

        public HashSet<string> Methods { get; private set; }

        public UriTemplate Template { get; private set; }

        public async Task ProcessAsync(HttpListenerContext context, ServerHttpTransport transport, UriTemplateMatch match, CancellationToken cancellationToken)
        {
            Identity owner;

            if (Identity.TryParse(context.User.Identity.Name, out owner))
            {
                var ids = await _envelopeStorage.GetEnvelopesAsync(owner).ConfigureAwait(false);
                if (ids.Length > 0)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.ContentType = Constants.TEXT_PLAIN_HEADER_VALUE;

                    using (var writer = new StreamWriter(context.Response.OutputStream))
                    {
                        foreach (var id in ids)
                        {
                            await writer.WriteLineAsync(id.ToString()).ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NoContent;                    
                }            
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;                
            }

            context.Response.Close();

        }

        #endregion
    }
}

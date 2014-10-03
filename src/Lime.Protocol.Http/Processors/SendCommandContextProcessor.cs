using Lime.Protocol.Network;
using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Lime.Protocol.Http.Serialization;

namespace Lime.Protocol.Http.Processors
{
    public sealed class SendCommandContextProcessor : SendEnvelopeRequestContextBase<Command>, IContextProcessor
    {
        #region Constructor

        public SendCommandContextProcessor(ConcurrentDictionary<Guid, HttpListenerResponse> pendingResponsesDictionary, ITraceWriter traceWriter = null)
            : base(new HashSet<string> { Constants.HTTP_METHOD_GET, Constants.HTTP_METHOD_POST, Constants.HTTP_METHOD_DELETE }, new UriTemplate(string.Format("/{0}/*", Constants.COMMANDS_PATH)), new DocumentSerializer(), pendingResponsesDictionary, traceWriter)
        {

        }

        #endregion

        #region SendEnvelopeRequestContextBase Members

        protected override async Task FillEnvelopeAsync(Command envelope, HttpListenerRequest request)
        {
            CommandMethod method;
            if (TryConvertToCommandMethod(request.HttpMethod, out method))
            {
                var limeUriFragment = request.Url.Segments.Except(new[] { Constants.COMMANDS_PATH + Constants.ROOT }).Aggregate((s1, s2) => s1 + s2);
                if (!string.IsNullOrWhiteSpace(limeUriFragment))
                {                    
                    if (envelope.Id == Guid.Empty)
                    {
                        envelope.Id = Guid.NewGuid();

                    }
                    envelope.Method = method;
                    envelope.Uri = new LimeUri(limeUriFragment);

                    if (envelope.Method == CommandMethod.Set ||
                        envelope.Method == CommandMethod.Observe)
                    {
                        envelope.Resource = await ParseDocumentAsync(request).ConfigureAwait(false);
                    }
                }
                else
                {
                    throw new LimeException(ReasonCodes.VALIDATION_INVALID_URI, "Invalid command URI");
                }
            }
            else
            {
                throw new LimeException(ReasonCodes.VALIDATION_INVALID_METHOD, "Invalid method");
            }
        }
        
        #endregion

        #region Private Methods

        private bool TryConvertToCommandMethod(string httpMethod, out CommandMethod commandMethod)
        {
            switch (httpMethod)
            {
                case Constants.HTTP_METHOD_GET:
                    commandMethod = CommandMethod.Get;
                    return true;
                case Constants.HTTP_METHOD_POST:
                    commandMethod = CommandMethod.Set;
                    return true;
                case Constants.HTTP_METHOD_DELETE:
                    commandMethod = CommandMethod.Delete;
                    return true;
                default:
                    commandMethod = default(CommandMethod);
                    return false;
            }
        }

        #endregion


    }
}

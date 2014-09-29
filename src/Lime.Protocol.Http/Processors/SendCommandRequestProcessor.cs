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
    public class SendCommandRequestProcessor : SendEnvelopeRequestProcessorBase<Command>, IRequestProcessor
    {
        #region Constructor

        public SendCommandRequestProcessor(ConcurrentDictionary<Guid, HttpListenerResponse> pendingResponsesDictionary)
            : base(new HashSet<string> { Constants.HTTP_METHOD_GET, Constants.HTTP_METHOD_POST, Constants.HTTP_METHOD_DELETE }, new UriTemplate(string.Format("/{0}", Constants.COMMANDS_PATH)), new DocumentSerializer(), pendingResponsesDictionary)
        {

        }

        #endregion

        protected override async Task<Command> ParseEnvelopeAsync(HttpListenerRequest request)
        {
            Command command = null;

            CommandMethod method;
            if (TryConvertToCommandMethod(request.HttpMethod, out method))
            {
                var limeUriFragment = request.Url.Segments.Except(new[] { Constants.COMMANDS_PATH + Constants.ROOT }).Aggregate((s1, s2) => s1 + s2);
                if (!string.IsNullOrWhiteSpace(limeUriFragment))
                {
                    command = new Command();
                    FillEnvelopeFromRequest(command, request);

                    if (command.Id != Guid.Empty)
                    {
                        command.Method = method;
                        command.Uri = new LimeUri(limeUriFragment);

                        if (command.Method == CommandMethod.Set ||
                            command.Method == CommandMethod.Observe)
                        {
                            command.Resource = await ParseDocumentAsync(request).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        throw new LimeException(ReasonCodes.VALIDATION_ERROR, "Invalid or empty id");
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

            return command;
        }

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

    }
}

using Lime.Protocol.Network;
using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Lime.Protocol.Http.Serialization;
using System.Threading;

namespace Lime.Protocol.Http.Processors
{
    public sealed class SendCommandHttpProcessor : SendEnvelopeHttpProcessorBase<Command>
    {
        #region Constructor

        public SendCommandHttpProcessor(ITraceWriter traceWriter = null)
            : base(new HashSet<string> { Constants.HTTP_METHOD_GET, Constants.HTTP_METHOD_POST, Constants.HTTP_METHOD_DELETE }, new UriTemplate(string.Format("/{0}/*", Constants.COMMANDS_PATH)), new DocumentSerializer(), traceWriter)
        {

        }

        #endregion

        public override async Task<HttpResponse> ProcessAsync(HttpRequest request, UriTemplateMatch match, IEmulatedTransport transport, CancellationToken cancellationToken)
        {
            bool isAsync;
            bool.TryParse(request.QueryString.Get(Constants.ASYNC_QUERY), out isAsync);

            if (isAsync)
            {
                return await base.ProcessAsync(request, match, transport, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var commandRequest = await GetEnvelopeFromRequestAsync(request).ConfigureAwait(false);
                try
                {
                    var commandResponse = await transport.ProcessCommandAsync(commandRequest, cancellationToken).ConfigureAwait(false);

                    string body = null;
                    string contentType = null;

                    if (commandResponse.Resource != null)
                    {
                        body = _serializer.Serialize(commandResponse.Resource);
                        contentType = commandResponse.Resource.GetMediaType().ToString();

                        if (_traceWriter != null &&
                            _traceWriter.IsEnabled)
                        {
                            await _traceWriter.TraceAsync(body, DataOperation.Send).ConfigureAwait(false);
                        }
                    }

                    if (commandResponse.Status == CommandStatus.Success)
                    {
                        return new HttpResponse(request.CorrelatorId, HttpStatusCode.OK, body: body, contentType: contentType);
                    }
                    else if (commandResponse.Reason != null)
                    {
                        return new HttpResponse(request.CorrelatorId, commandResponse.Reason.ToHttpStatusCode(), commandResponse.Reason.Description, body: body, contentType: contentType);
                    }
                    else
                    {
                        return new HttpResponse(request.CorrelatorId, HttpStatusCode.ServiceUnavailable, body: body);
                    }
                }
                catch (ArgumentException)
                {
                    return new HttpResponse(request.CorrelatorId, HttpStatusCode.Conflict);
                }
            }            
        }

        #region SendEnvelopeRequestContextBase Members

        protected override async Task FillEnvelopeAsync(Command envelope, HttpRequest request)
        {
            CommandMethod method;
            if (TryConvertToCommandMethod(request.Method, out method))
            {
                var limeUriFragment = request.Uri.Segments.Except(new[] { Constants.COMMANDS_PATH + Constants.ROOT }).Aggregate((s1, s2) => s1 + s2);
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

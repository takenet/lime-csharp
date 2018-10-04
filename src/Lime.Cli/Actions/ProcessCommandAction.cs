using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Cli.Actions
{
    public class ProcessCommandAction : ActionBase<ProcessCommandOptions>
    {
        private readonly IDocumentSerializer _documentSerializer;

        public ProcessCommandAction(IDocumentSerializer documentSerializer)
        {
            _documentSerializer = documentSerializer;
        }

        protected override async Task ExecuteAsync(ProcessCommandOptions options, IEstablishedChannel channel, CancellationToken cancellationToken)
        {
            if (options.ExpectedResource != null && options.Method != CommandMethod.Get)
            {
                throw new Exception("The expected resource option can only be used with 'get' method");
            }

            var resource = _documentSerializer.Deserialize(options.JoinedResource, options.Type);

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(options.Timeout)))
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken))
            {
                var responseCommand = await channel.ProcessCommandAsync(
                    new Command(options.Id ?? EnvelopeId.NewId())
                    {
                        From = options.From,
                        To = options.To,
                        Method = options.Method,
                        Uri = new LimeUri(options.Uri),
                        Resource = resource
                    },
                    cancellationToken);

                if (responseCommand.Status != options.ExpectedStatus)
                {
                    throw new Exception($"Unexpected response status '{responseCommand.Status}' ({responseCommand.Reason?.ToString()})");
                }

                if (options.ExpectedResource != null)
                {
                    var expectedResourceRegex = new Regex(options.ExpectedResource);
                    var responseResource = _documentSerializer.Serialize(responseCommand.Resource);

                    if (!expectedResourceRegex.IsMatch(responseResource))
                    {
                        throw new Exception($"Unexpected response resource: {responseResource}");
                    }
                }
            }
        }
    }
}

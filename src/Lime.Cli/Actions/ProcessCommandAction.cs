using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using System;
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
            }
        }
    }
}

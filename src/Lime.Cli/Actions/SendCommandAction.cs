using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Cli.Actions
{
    public class SendCommandAction : ActionBase<SendCommandOptions>
    {
        private readonly IDocumentSerializer _documentSerializer;

        public SendCommandAction(IDocumentSerializer documentSerializer)
        {
            _documentSerializer = documentSerializer;
        }

        protected override Task ExecuteAsync(SendCommandOptions options, IEstablishedChannel channel, CancellationToken cancellationToken)
        {
            var resource = _documentSerializer.Deserialize(options.JoinedResource, options.Type);

            return channel.SendCommandAsync(
                new Command(options.Id ?? EnvelopeId.NewId())
                {
                    From = options.From,
                    To = options.To,
                    Method = options.Method,
                    Uri = new LimeUri(options.Uri),
                    Resource = resource
                },
                cancellationToken);
        }
    }
}

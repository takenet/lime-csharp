using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Cli.Actions
{
    public class SendMessageAction : ActionBase<SendMessageOptions>
    {
        private readonly IDocumentSerializer _documentSerializer;

        public SendMessageAction(IDocumentSerializer documentSerializer)
        {
            _documentSerializer = documentSerializer;
        }

        protected override Task ExecuteAsync(SendMessageOptions options, IEstablishedChannel channel, CancellationToken cancellationToken)
        {
            var content = _documentSerializer.Deserialize(options.JoinedContent, options.Type);

            return channel.SendMessageAsync(
                new Message(options.Id)
                {
                    From = options.From,
                    To = options.To,
                    Content = content
                },
                cancellationToken);

        }
    }


}

using System.Threading;
using System.Threading.Tasks;
using Lime.Messaging.Resources;
using Lime.Protocol;
using Lime.Protocol.Network;

namespace Lime.Cli.Actions
{
    public class CreateAccountAction : ActionBase<CreateAccountOptions>
    {
        protected override Task ExecuteAsync(CreateAccountOptions options, IEstablishedChannel channel, CancellationToken cancellationToken)
        {
            var identity = Identity.Parse(options.Identity);
            
            return channel.SetResourceAsync(
                new LimeUri($"lime://{identity}{UriTemplates.ACCOUNT}"),
                new Account()
                {
                    Identity = identity,
                    Password = options.Password.ToBase64(),
                    FullName = options.Name
                },
                cancellationToken);
        }
    }
}
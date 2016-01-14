using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    public abstract class ChannelModuleBase<T> : IChannelModule<T> where T : Envelope, new()
    {
        public virtual Task<T> OnReceiving(T envelope, CancellationToken cancellationToken)
        {
            return Task.FromResult(envelope);
        }

        public virtual Task<T> OnSending(T envelope, CancellationToken cancellationToken)
        {
            return Task.FromResult(envelope);
        }
    }
}
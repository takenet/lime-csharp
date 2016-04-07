using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network.Modules
{
    public abstract class ChannelModuleBase<T> : IChannelModule<T> where T : Envelope, new()
    {
        public virtual void OnStateChanged(SessionState state)
        {
            
        }

        public virtual Task<T> OnReceivingAsync(T envelope, CancellationToken cancellationToken)
        {
            return Task.FromResult(envelope);
        }

        public virtual Task<T> OnSendingAsync(T envelope, CancellationToken cancellationToken)
        {
            return Task.FromResult(envelope);
        }
    }
}
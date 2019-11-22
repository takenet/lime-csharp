using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    public interface IFlushable
    {
        /// <summary>
        /// Flushes the channel send buffer. 
        /// </summary>
        Task FlushAsync(CancellationToken cancellationToken);
    }
}
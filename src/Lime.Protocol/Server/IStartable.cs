using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Server
{
    public interface IStartable
    {
        /// <summary>
        /// Start listening for connections.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task StartAsync(CancellationToken cancellationToken);
    }

    public static class StartableExtensions
    {        
        /// <summary>
        /// Start listening for connections.
        /// </summary>             
        /// <returns></returns>
        public static Task StartAsync(this IStartable startable) => startable.StartAsync(CancellationToken.None);
    }
}
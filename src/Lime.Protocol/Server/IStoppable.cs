using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Server
{
    public interface IStoppable
    {
        /// <summary>
        /// Stops the tranport listener.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task StopAsync(CancellationToken cancellationToken);
    }

    public static class StoppableExtensions
    {
        /// <summary>
        /// Stops the tranport listener.
        /// </summary>             
        /// <returns></returns>
        public static Task StopAsync(this IStoppable stoppable) => stoppable.StopAsync(CancellationToken.None);
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines a proxy interface for executing actions to the envelope channels.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IChannelModule<T> where T: Envelope, new()
    {
        /// <summary>
        /// Called when the session state is changed.
        /// </summary>
        /// <param name="state">The state.</param>
        void OnStateChanged(SessionState state);

        /// <summary>
        /// Called when an envelope is being received by the channel.
        /// </summary>
        /// <param name="envelope">The envelope.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The received envelope. If the value is null, the received envelope will be ignored.</returns>
        Task<T> OnReceivingAsync(T envelope, CancellationToken cancellationToken);

        /// <summary>
        /// Called when an envelope is being sent by the channel.
        /// </summary>
        /// <param name="envelope">The envelope.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The envelope to be sent. If the value is null, the sent envelope will be ignored.</returns>
        Task<T> OnSendingAsync(T envelope, CancellationToken cancellationToken);
    }
}
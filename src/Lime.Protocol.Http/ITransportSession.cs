using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Http
{
    /// <summary>
    /// Defines a transport-level 
    /// session emulation.
    /// </summary>
    public interface ITransportSession
    {
        /// <summary>
        /// Gets the session expiration.
        /// </summary>
        /// <value>
        /// The expiration.
        /// </value>
        DateTimeOffset Expiration { get; }

        /// <summary>
        /// Gets the result session
        /// of the authentication proccess.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<Session> GetSessionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Finishes the transport session..
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task FinishAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Submits the envelope to the
        /// underlying transport.
        /// </summary>
        /// <param name="envelope">The envelope.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task SubmitAsync(Envelope envelope, CancellationToken cancellationToken);

        /// <summary>
        /// Submits the message to the
        /// underlying transport and awaits
        /// for the first related notification.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<Notification> ProcessMessageAsync(Message message, CancellationToken cancellationToken);

        /// <summary>
        /// Submits the command to the
        /// underlying transport and awaits
        /// for the response.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<Command> ProcessCommandAsync(Command command, CancellationToken cancellationToken);
    }
}

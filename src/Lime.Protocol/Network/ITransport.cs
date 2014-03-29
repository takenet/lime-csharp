using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines a network 
    /// connection with a node
    /// </summary>
    public interface ITransport
    {
        /// <summary>
        /// Sends an envelope to 
        /// the connected node
        /// </summary>
        /// <param name="envelope">Envelope to be transported</param>
        /// <returns></returns>
        Task SendAsync(Envelope envelope);

        /// <summary>
        /// Occurs when an envelope
        /// is received by the node
        /// </summary>
        event EventHandler<EnvelopeEventArgs<Envelope>> EnvelopeReceived;

        /// <summary>
        /// Closes the connection
        /// </summary>
        Task CloseAsync();

        /// <summary>
        /// Occurs when the connection fails
        /// </summary>
        event EventHandler<ExceptionEventArgs> Failed;

        /// <summary>
        /// Occurs when the channel is about
        /// to be closed
        /// </summary>
        event EventHandler<DeferralEventArgs> Closing;

        /// <summary>
        /// Occurs after the connection was closed
        /// </summary>
        event EventHandler Closed;
    }
}

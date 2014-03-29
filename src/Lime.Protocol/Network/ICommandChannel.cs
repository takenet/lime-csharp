using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines a command envelopes exchanging channel
    /// </summary>
    public interface ICommandChannel
    {
        /// <summary>
        /// Sends a command envelope to
        /// the remote node
        /// </summary>
        /// <param name="command"></param>
        Task SendCommandAsync(Command command);

        /// <summary>
        /// Occurs when a command envelope 
        /// is received by the node
        /// </summary>
        event EventHandler<EnvelopeEventArgs<Command>> CommandReceived;
    }
}

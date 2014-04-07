using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Tcp
{
    /// <summary>
    /// Encapsulates the TcpClient methods used
    /// by the transport.
    /// </summary>
    public interface ITcpClient
    {
        /// <summary>
        /// Returns the System.Net.Sockets.NetworkStream used to send and receive data.
        /// </summary>
        /// <returns></returns>
        Stream GetStream();

        /// <summary>
        /// Connects the client to the specified port on the specified host.
        /// </summary>
        /// <param name="address">The DNS name of the remote host to which you intend to connect.</param>
        /// <param name="port">The portThe port number of the remote host to which you intend to connect.</param>
        /// <returns></returns>
        Task ConnectAsync(string host, int port);

        /// <summary>
        /// Gets a value indicating whether the underlying System.Net.Sockets.Socket
        /// for a System.Net.Sockets.TcpClient is connected to a remote host.
        /// </summary>
        /// <value>
        ///   <c>true</c> if connected; otherwise, <c>false</c>.
        /// </value>
        bool Connected { get; }

        /// <summary>
        /// Disposes this System.Net.Sockets.TcpClient instance and requests that the
        /// underlying TCP connection be closed.
        /// </summary>
        void Close();
    }
}

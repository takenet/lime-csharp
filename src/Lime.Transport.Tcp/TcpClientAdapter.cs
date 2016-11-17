using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Policy;
using System.Threading.Tasks;

namespace Lime.Transport.Tcp
{
    public sealed class TcpClientAdapter : ITcpClient
    {
        private readonly TcpClient _tcpClient;

        public TcpClientAdapter(TcpClient tcpClient)
        {
            if (tcpClient == null) throw new ArgumentNullException(nameof(tcpClient));            
            _tcpClient = tcpClient;
        }

        /// <summary>
        /// Returns the System.Net.Sockets.NetworkStream used to send and receive data.
        /// </summary>
        /// <returns></returns>
        public Stream GetStream() => Stream.Synchronized(_tcpClient.GetStream());

        /// <summary>
        /// Connects the client to the specified port on the specified host.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port">The portThe port number of the remote host to which you intend to connect.</param>
        /// <returns></returns>
        public Task ConnectAsync(string host, int port) => _tcpClient.ConnectAsync(host, port);

        /// <summary>
        /// Gets a value indicating whether the underlying System.Net.Sockets.Socket
        /// for a System.Net.Sockets.TcpClient is connected to a remote host.
        /// </summary>
        /// <value>
        ///   <c>true</c> if connected; otherwise, <c>false</c>.
        /// </value>
        public bool Connected => _tcpClient.Connected;

        /// <summary>
        /// Disposes this System.Net.Sockets.TcpClient instance and requests that the
        /// underlying TCP connection be closed.
        /// </summary>
        public void Close() => _tcpClient.Close();

        /// <summary>
        /// Used by the class to provide the underlying network socket. 
        /// </summary>
        /// <value>
        /// The client.
        /// </value>
        public Socket Client => _tcpClient.Client;
    }
}

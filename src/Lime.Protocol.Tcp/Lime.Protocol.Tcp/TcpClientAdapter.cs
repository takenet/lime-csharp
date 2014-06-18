using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Tcp
{
    public sealed class TcpClientAdapter : ITcpClient
    {
        private readonly TcpClient _tcpClient;

        public TcpClientAdapter(TcpClient tcpClient)
        {
            if (tcpClient == null)
            {
                throw new ArgumentNullException("tcpClient");
            }

            _tcpClient = tcpClient;
        }

        #region ITcpClient Members

        /// <summary>
        /// Returns the System.Net.Sockets.NetworkStream used to send and receive data.
        /// </summary>
        /// <returns></returns>
        public System.IO.Stream GetStream()
        {
            return _tcpClient.GetStream();
        }

        /// <summary>
        /// Connects the client to the specified port on the specified host.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port">The portThe port number of the remote host to which you intend to connect.</param>
        /// <returns></returns>
        public Task ConnectAsync(string host, int port)
        {
            return _tcpClient.ConnectAsync(host, port);
        }

        /// <summary>
        /// Gets a value indicating whether the underlying System.Net.Sockets.Socket
        /// for a System.Net.Sockets.TcpClient is connected to a remote host.
        /// </summary>
        /// <value>
        ///   <c>true</c> if connected; otherwise, <c>false</c>.
        /// </value>
        public bool Connected
        {
            get { return _tcpClient.Connected; }
        }

        public void Close()
        {
            _tcpClient.Close();
        }

        #endregion
    }
}

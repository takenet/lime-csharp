using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Lime.Transport.Tcp;
using Microsoft.AspNetCore.Connections;

namespace Lime.Transport.AspNetCore
{
    public sealed class ConnectionContextTcpClientAdapter : ITcpClient
    {
        private readonly ConnectionContext _context;

        public ConnectionContextTcpClientAdapter(ConnectionContext context)
        {
            _context = context;
        }
        
        public Stream GetStream() => new DuplexPipeStreamAdapter(_context.Transport);

        public Task ConnectAsync(string host, int port) => Task.CompletedTask;

        public bool Connected { get; } = true;
        public void Close()
        {
            
        }

        public Socket Client { get; } = null!;
    }
}
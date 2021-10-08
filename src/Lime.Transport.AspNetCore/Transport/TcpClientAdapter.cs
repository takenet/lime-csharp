using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Lime.Transport.Tcp;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

namespace Lime.Transport.AspNetCore.Transport
{
    internal sealed class TcpClientAdapter : ITcpClient
    {
        private readonly ConnectionContext _context;
        private bool _closed;

        public TcpClientAdapter(ConnectionContext context)
        {
            _context = context;
        }
        
        public Stream GetStream() => new DuplexPipeStream(_context.Transport.Input, _context.Transport.Output);

        public Task ConnectAsync(string host, int port) => Task.CompletedTask;

        public bool Connected => !_context.ConnectionClosed.IsCancellationRequested && !_closed;

        public void Close()
        {
            _context.Transport.Input.Complete();
            _context.Transport.Output.Complete();
            _closed = true;
        }

        public Socket Client => null!;
    }
}
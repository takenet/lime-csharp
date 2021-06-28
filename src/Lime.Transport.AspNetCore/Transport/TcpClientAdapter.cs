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

        public TcpClientAdapter(ConnectionContext context)
        {
            _context = context;
        }
        
        public Stream GetStream() => new DuplexPipeStream(_context.Transport.Input, _context.Transport.Output);

        public Task ConnectAsync(string host, int port) => Task.CompletedTask;

        public bool Connected => !_context.ConnectionClosed.IsCancellationRequested;

        public void Close()
        {
            _context.Transport.Input.Complete();
            _context.Transport.Output.Complete();
        }

        public Socket Client => null!;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Util;

namespace Lime.Protocol.Server
{
    /// <summary>
    /// Implements a <see cref="ITransportListener"/> aggregator that allows multiple transports to be listened as one.
    /// </summary>
    public sealed class AggregateTransportListener : ITransportListener, IDisposable
    {
        private readonly IEnumerable<ITransportListener> _transportListeners;
        private readonly Channel<ITransport> _transportChannel;
        private readonly List<Task> _listenerTasks;
        private readonly CancellationTokenSource _cts;

        public AggregateTransportListener(IEnumerable<ITransportListener> transportListeners, int capacity = -1)
        {
            _transportListeners = transportListeners;
            ListenerUris = transportListeners.SelectMany(t => t.ListenerUris).ToArray();
            _transportChannel = ChannelUtil.CreateForCapacity<ITransport>(capacity);
            _listenerTasks = new List<Task>();
            _cts = new CancellationTokenSource();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var transportListener in _transportListeners)
            {
                await transportListener.StartAsync(cancellationToken);
                
                var t = transportListener;
                _listenerTasks.Add(
                    Task.Run(() => ListenAsync(t, _cts.Token)));
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.CancelIfNotRequested();

            return Task.WhenAll(
                _listenerTasks
                    .Union(
                        _transportListeners.Select(t 
                            => t.StopAsync(cancellationToken))));
        }

        public Uri[] ListenerUris { get; }

        public Task<ITransport> AcceptTransportAsync(CancellationToken cancellationToken) 
            => _transportChannel.Reader.ReadAsync(cancellationToken).AsTask();

        private async Task ListenAsync(ITransportListener transportListener, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var transport = await transportListener.AcceptTransportAsync(cancellationToken);
                    await _transportChannel.Writer.WriteAsync(transport, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        public void Dispose()
        {
            _cts.Dispose();
        }
    }
}
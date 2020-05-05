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
        private readonly IReadOnlyCollection<ITransportListener> _transportListeners;
        private readonly Channel<ITransport> _transportChannel;
        private readonly List<Task> _listenerTasks;
        private readonly CancellationTokenSource _cts;
        private bool _listenerFaulted;

        public AggregateTransportListener(IEnumerable<ITransportListener> transportListeners, int capacity = -1)
        {
            if (transportListeners == null) throw new ArgumentNullException(nameof(transportListeners));
            _transportListeners = transportListeners.ToList();
            if (_transportListeners.Count == 0)
            {
                throw new ArgumentException("The transport listeners enumerable is empty", nameof(transportListeners));
            }
            
            ListenerUris = _transportListeners.SelectMany(t => t.ListenerUris).ToArray();
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

                var listenerTask = Task.Run(() => ListenAsync(t, _cts.Token));
                _listenerTasks.Add(listenerTask);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.CancelIfNotRequested();
            try
            {
                foreach (var transportListener in _transportListeners)
                {
                    await transportListener.StopAsync(cancellationToken);
                }
            }
            finally
            {
                await Task.WhenAll(_listenerTasks);
            }
        }

        public Uri[] ListenerUris { get; }

        public Task<ITransport> AcceptTransportAsync(CancellationToken cancellationToken)
        {
            if (_listenerFaulted)
            {
                var exceptions = _listenerTasks
                    .Where(t => t.Exception != null)
                    .SelectMany(t => t.Exception.InnerExceptions)
                    .ToList();

                if (exceptions.Count > 0)
                {
                    return Task.FromException<ITransport>(new AggregateException(exceptions));
                }
            }
            
            return _transportChannel.Reader.ReadAsync(cancellationToken).AsTask();
        }

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
                catch
                {
                    _listenerFaulted = true;
                    throw;
                }
            }
        }

        public void Dispose()
        {
            _cts.Dispose();
        }
    }
}
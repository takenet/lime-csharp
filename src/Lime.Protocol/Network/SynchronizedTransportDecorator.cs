using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines a decorator for <see cref="ITransport"/> that synchronizes concurrent <see cref="SendAsync"/> and <see cref="ReceiveAsync"/> calls.
    /// </summary>
    public sealed class SynchronizedTransportDecorator : ITransport
    {
        private readonly ITransport _transport;
        private readonly SemaphoreSlim _sendSemaphore;
        private readonly SemaphoreSlim _receiveSemaphore;

        public SynchronizedTransportDecorator(ITransport transport)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _sendSemaphore = new SemaphoreSlim(1);
            _receiveSemaphore = new SemaphoreSlim(1);
        }

        public SessionCompression Compression => _transport.Compression;

        public SessionEncryption Encryption => _transport.Encryption;

        public bool IsConnected => _transport.IsConnected;

        public string LocalEndPoint => _transport.LocalEndPoint;

        public string RemoteEndPoint => _transport.RemoteEndPoint;

        public IReadOnlyDictionary<string, object> Options => _transport.Options;

        public async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            await _sendSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _transport.SendAsync(envelope, cancellationToken);
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        public async Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            await _receiveSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await _transport.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _receiveSemaphore.Release();
            }
        }

        public Task OpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            return _transport.OpenAsync(uri, cancellationToken);
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            return _transport.CloseAsync(cancellationToken);
        }

        public SessionCompression[] GetSupportedCompression()
        {
            return _transport.GetSupportedCompression();
        }

        public Task SetCompressionAsync(SessionCompression compression, CancellationToken cancellationToken)
        {
            return _transport.SetCompressionAsync(compression, cancellationToken);
        }

        public SessionEncryption[] GetSupportedEncryption()
        {
            return _transport.GetSupportedEncryption();
        }

        public Task SetEncryptionAsync(SessionEncryption encryption, CancellationToken cancellationToken)
        {
            return _transport.SetEncryptionAsync(encryption, cancellationToken);
        }

        public Task SetOptionAsync(string name, object value)
        {
            return _transport.SetOptionAsync(name, value);
        }

        public event EventHandler<DeferralEventArgs> Closing
        {
            add => _transport.Closing += value;
            remove => _transport.Closing -= value;
        }

        public event EventHandler Closed
        {
            add => _transport.Closed += value;
            remove => _transport.Closed -= value;
        }
    }
}
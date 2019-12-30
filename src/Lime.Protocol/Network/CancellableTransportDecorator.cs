using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines a decorator for <see cref="ITransport"/> that implements cancellation when it is not supported by the underlying instance.
    /// </summary>
    public sealed class CancellableTransportDecorator : ITransport, IDisposable
    {
        private readonly ITransport _transport;
        private readonly IDisposable _disposableTransport;

        public CancellableTransportDecorator(ITransport transport, bool disposeIfDisposable = false)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            if (disposeIfDisposable)
            {
                _disposableTransport = transport as IDisposable;
            }
        }

        public SessionCompression Compression => _transport.Compression;

        public SessionEncryption Encryption => _transport.Encryption;

        public bool IsConnected => _transport.IsConnected;

        public string LocalEndPoint => _transport.LocalEndPoint;

        public string RemoteEndPoint => _transport.RemoteEndPoint;

        public IReadOnlyDictionary<string, object> Options => _transport.Options;

        public Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            return _transport.SendAsync(envelope, cancellationToken).WithCancellation(cancellationToken);
        }

        public Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            return _transport.ReceiveAsync(cancellationToken).WithCancellation(cancellationToken);
        }

        public Task OpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            return _transport.OpenAsync(uri, cancellationToken).WithCancellation(cancellationToken);
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            return _transport.CloseAsync(cancellationToken).WithCancellation(cancellationToken);
        }

        public SessionCompression[] GetSupportedCompression()
        {
            return _transport.GetSupportedCompression();
        }

        public Task SetCompressionAsync(SessionCompression compression, CancellationToken cancellationToken)
        {
            return _transport.SetCompressionAsync(compression, cancellationToken).WithCancellation(cancellationToken);
        }

        public SessionEncryption[] GetSupportedEncryption()
        {
            return _transport.GetSupportedEncryption();
        }

        public Task SetEncryptionAsync(SessionEncryption encryption, CancellationToken cancellationToken)
        {
            return _transport.SetEncryptionAsync(encryption, cancellationToken).WithCancellation(cancellationToken);
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

        public void Dispose()
        {
            _disposableTransport?.Dispose();
        }
    }
}
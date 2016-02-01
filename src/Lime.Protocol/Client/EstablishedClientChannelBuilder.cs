using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Security;

namespace Lime.Protocol.Client
{
    public sealed class EstablishedClientChannelBuilder
    {
        private readonly ClientChannelBuilder _clientChannelBuilder;
        private readonly Identity _identity;
        private readonly List<Func<IClientChannel, CancellationToken, Task>> _establishedHandlers;

        private Func<SessionCompression[], SessionCompression> _compressionSelector;
        private Func<SessionEncryption[], SessionEncryption> _encryptionSelector;       
        private Func<AuthenticationScheme[], Authentication, Authentication> _authenticator;
        private string _instance;        

        internal EstablishedClientChannelBuilder(ClientChannelBuilder clientChannelBuilder, Identity identity)
        {
            if (clientChannelBuilder == null) throw new ArgumentNullException(nameof(clientChannelBuilder));
            if (identity == null) throw new ArgumentNullException(nameof(identity));
            _clientChannelBuilder = clientChannelBuilder;
            _identity = identity;
            _establishedHandlers = new List<Func<IClientChannel, CancellationToken, Task>>();
            _compressionSelector = options => options.First();
            _encryptionSelector = options => options.First();
            _authenticator = (options, roundtrip) => new GuestAuthentication();
            _instance = Environment.MachineName;
        }

        /// <summary>
        /// Sets the compression options to be used in the session establishment.
        /// </summary>
        /// <param name="compression">The compression.</param>
        /// <returns></returns>
        public EstablishedClientChannelBuilder WithCompression(SessionCompression compression)
        {
            return WithCompression(options => compression);
        }

        /// <summary>
        /// Sets the compression options to be used in the session establishment.
        /// </summary>
        /// <param name="compressionSelector">The compression selector.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public EstablishedClientChannelBuilder WithCompression(
            Func<SessionCompression[], SessionCompression> compressionSelector)
        {
            if (compressionSelector == null) throw new ArgumentNullException(nameof(compressionSelector));
            _compressionSelector = compressionSelector;
            return this;
        }

        /// <summary>
        /// Sets the encryption options to be used in the session establishment.
        /// </summary>
        /// <param name="encryption">The encryption.</param>
        /// <returns></returns>
        public EstablishedClientChannelBuilder WithEncryption(SessionEncryption encryption)
        {
            return WithEncryption(options => encryption);
        }

        /// <summary>
        /// Sets the encryption options to be used in the session establishment.
        /// </summary>
        /// <param name="encryptionSelector">The encryption selector.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public EstablishedClientChannelBuilder WithEncryption(
            Func<SessionEncryption[], SessionEncryption> encryptionSelector)
        {
            if (encryptionSelector == null) throw new ArgumentNullException(nameof(encryptionSelector));
            _encryptionSelector = encryptionSelector;
            return this;
        }

        /// <summary>
        /// Sets the authentication password to be used in the session establishment.
        /// </summary>
        /// <param name="password">The authentication password.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public EstablishedClientChannelBuilder WithPlainAuthentication(string password)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentException("Argument is null or empty", nameof(password));
            var authentication = new PlainAuthentication();
            authentication.SetToBase64Password(password);
            return WithAuthentication(authentication);
        }

        /// <summary>
        /// Sets the authentication key to be used in the session establishment.
        /// </summary>
        /// <param name="key">The authentication key.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public EstablishedClientChannelBuilder WithKeyAuthentication(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Argument is null or empty", nameof(key));
            var authentication = new KeyAuthentication();
            authentication.SetToBase64Key(key);
            return WithAuthentication(authentication);
        }

        /// <summary>
        /// Sets the authentication to be used in the session establishment.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public EstablishedClientChannelBuilder WithAuthentication<TAuthentication>() where TAuthentication : Authentication, new()
        {
            return WithAuthentication((schemes, roundtrip) => new TAuthentication());
        }

        /// <summary>
        /// Sets the authentication to be used in the session establishment.
        /// </summary>
        /// <param name="authentication">The authentication.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public EstablishedClientChannelBuilder WithAuthentication(Authentication authentication)
        {
            if (authentication == null) throw new ArgumentNullException(nameof(authentication));
            return WithAuthentication((schemes, roundtrip) => authentication);
        }

        /// <summary>
        /// Sets the authentication to be used in the session establishment.
        /// </summary>
        /// <param name="authenticator">The authenticator.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public EstablishedClientChannelBuilder WithAuthentication(
            Func<AuthenticationScheme[], Authentication, Authentication> authenticator)
        {
            if (authenticator == null) throw new ArgumentNullException(nameof(authenticator));
            _authenticator = authenticator;
            return this;
        }

        /// <summary>
        /// Sets the instance name to be used in the session establishment.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public EstablishedClientChannelBuilder WithInstance(string instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            _instance = instance;
            return this;
        }

        /// <summary>
        /// Adds a handler to be executed after the channel is built and established.
        /// </summary>
        /// <param name="establishedHandler">The handler to be executed.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public EstablishedClientChannelBuilder AddEstablishedHandler(Func<IClientChannel, CancellationToken, Task> establishedHandler)
        {
            if (establishedHandler == null) throw new ArgumentNullException(nameof(establishedHandler));
            _establishedHandlers.Add(establishedHandler);
            return this;
        }

        /// <summary>
        /// Builds a <see cref="ClientChannel"/> instance and establish the session using the builder options.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<IClientChannel> BuildAndEstablishAsync(CancellationToken cancellationToken)
        {
            var clientChannel = await _clientChannelBuilder
                .BuildAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                var session = await clientChannel.EstablishSessionAsync(
                    _compressionSelector,
                    _encryptionSelector,
                    _identity,
                    _authenticator,
                    _instance,
                    cancellationToken)
                    .ConfigureAwait(false);

                if (session.State != SessionState.Established)
                {
                    var reason = session.Reason ?? new Reason()
                    {
                        Code = ReasonCodes.GENERAL_ERROR,
                        Description = "Could not establish the session for unknown reason"
                    };

                    throw new LimeException(reason);
                }
                foreach (var handler in _establishedHandlers.ToList())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await handler(clientChannel, cancellationToken).ConfigureAwait(false);
                }
            }
            catch 
            {
                clientChannel.DisposeIfDisposable();
                throw;
            }

            return clientChannel;
        }
    }
}
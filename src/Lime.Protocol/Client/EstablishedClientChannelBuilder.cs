using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Security;

namespace Lime.Protocol.Client
{
    /// <summary>
    /// Helper class for building instances of <see cref="ClientChannel"/> and handling the establishment of the session for the channel.
    /// </summary>
    public sealed class EstablishedClientChannelBuilder : IEstablishedClientChannelBuilder
    {
        private readonly List<Func<IClientChannel, CancellationToken, Task>> _establishedHandlers;
        private Func<SessionCompression[], SessionCompression> _compressionSelector;
        private Func<SessionEncryption[], SessionEncryption> _encryptionSelector;
        private Func<AuthenticationScheme[], Authentication, Authentication> _authenticator;

        /// <summary>
        /// Initializes a new instance of the <see cref="EstablishedClientChannelBuilder"/> class.
        /// </summary>
        /// <param name="clientChannelBuilder">The client channel builder.</param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public EstablishedClientChannelBuilder(IClientChannelBuilder clientChannelBuilder)
        {
            if (clientChannelBuilder == null) throw new ArgumentNullException(nameof(clientChannelBuilder));
            ChannelBuilder = clientChannelBuilder;
            _establishedHandlers = new List<Func<IClientChannel, CancellationToken, Task>>();
            _compressionSelector = options => options.First();
            _encryptionSelector = options => options.First();
            _authenticator = (options, roundtrip) => new GuestAuthentication();
            EstablishmentTimeout = TimeSpan.FromSeconds(30);
            Identity = new Identity(Guid.NewGuid().ToString(), clientChannelBuilder.ServerUri.Host);
            Instance = Environment.MachineName;
        }

        /// <summary>
        /// Gets the associated channel builder.
        /// </summary>
        public IClientChannelBuilder ChannelBuilder { get; }

        /// <summary>
        /// Gets the identity.
        /// </summary>        
        public Identity Identity { get; private set; }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public string Instance { get; private set; }

        /// <summary>
        /// Gets the establishment timeout
        /// </summary>
        public TimeSpan EstablishmentTimeout { get; private set; }

        /// <summary>
        /// Sets the timeout to Build and establish a new session
        /// </summary>
        /// <param name="timeout">The timeout</param>
        /// <returns></returns>
        public EstablishedClientChannelBuilder WithEstablishmentTimeout(TimeSpan timeout)
        {
            EstablishmentTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets the compression option to be used in the session establishment.
        /// </summary>
        /// <param name="compression">The compression.</param>
        /// <returns></returns>
        public EstablishedClientChannelBuilder WithCompression(SessionCompression compression)
        {
            return WithCompression(options => compression);
        }

        /// <summary>
        /// Sets the compression selector to be used in the session establishment.
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
        /// Sets the encryption option to be used in the session establishment.
        /// </summary>
        /// <param name="encryption">The encryption.</param>
        /// <returns></returns>
        public EstablishedClientChannelBuilder WithEncryption(SessionEncryption encryption)
        {
            return WithEncryption(options => encryption);
        }

        /// <summary>
        /// Sets the encryption selector to be used in the session establishment.
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
        /// <param name="password">The authentication password, in plain text.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public EstablishedClientChannelBuilder WithPlainAuthentication(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
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
            if (key == null) throw new ArgumentNullException(nameof(key));
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
        /// Sets the identity to be used in the session establishment.
        /// </summary>
        /// <param name="identity">The identity to be used.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public EstablishedClientChannelBuilder WithIdentity(Identity identity)
        {
            if (identity == null) throw new ArgumentNullException(nameof(identity));
            Identity = identity;
            return this;
        }

        /// <summary>
        /// Sets the instance name to be used in the session establishment.
        /// </summary>
        /// <param name="instance">The instance to be used.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public EstablishedClientChannelBuilder WithInstance(string instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            Instance = instance;
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
            var clientChannel = await ChannelBuilder
                .BuildAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {

                Session session;

                using (var cancellationTokenSource = new CancellationTokenSource(EstablishmentTimeout))
                {
                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token, cancellationToken))
                    {
                        session = await clientChannel.EstablishSessionAsync(
                            _compressionSelector,
                            _encryptionSelector,
                            Identity,
                            _authenticator,
                            Instance,
                            linkedCts.Token)
                            .ConfigureAwait(false);
                    }
                }

                if (session.State != SessionState.Established)
                {
                    var reason = session.Reason ?? new Reason()
                    {
                        Code = ReasonCodes.GENERAL_ERROR,
                        Description = "Could not establish the session for an unknown reason"
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
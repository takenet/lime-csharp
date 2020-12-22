using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="EstablishedClientChannelBuilder"/> class.
        /// </summary>
        /// <param name="clientChannelBuilder">The client channel builder.</param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public EstablishedClientChannelBuilder(IClientChannelBuilder clientChannelBuilder)
        {
            ChannelBuilder = clientChannelBuilder ?? throw new ArgumentNullException(nameof(clientChannelBuilder));
            _establishedHandlers = new List<Func<IClientChannel, CancellationToken, Task>>();
            CompressionSelector = options => options.First();
            EncryptionSelector = options => options.First();
            Authenticator = (options, roundtrip) => new GuestAuthentication();
            EstablishmentTimeout = TimeSpan.FromSeconds(30);
            Identity = new Identity(EnvelopeId.NewId(), clientChannelBuilder.ServerUri.Host);
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
        /// Gets the compression selector.
        /// </summary>
        public Func<SessionCompression[], SessionCompression> CompressionSelector { get; private set; }

        /// <summary>
        /// Gets the encryption selector.
        /// </summary>
        public Func<SessionEncryption[], SessionEncryption> EncryptionSelector { get; private set; }

        /// <summary>
        /// Gets the authenticator.
        /// </summary>
        public Func<AuthenticationScheme[], Authentication, Authentication> Authenticator { get; private set; }

        /// <summary>
        /// Gets the established handlers.
        /// </summary>
        public IEnumerable<Func<IClientChannel, CancellationToken, Task>> EstablishedHandlers => _establishedHandlers.AsReadOnly();

        /// <summary>
        /// Sets the timeout to Build and establish a new session
        /// </summary>
        /// <param name="timeout">The timeout</param>
        /// <returns></returns>
        public IEstablishedClientChannelBuilder WithEstablishmentTimeout(TimeSpan timeout)
        {
            EstablishmentTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets the compression option to be used in the session establishment.
        /// </summary>
        /// <param name="compression">The compression.</param>
        /// <returns></returns>
        public IEstablishedClientChannelBuilder WithCompression(SessionCompression compression)
        {
            return WithCompression(options => compression);
        }

        /// <summary>
        /// Sets the compression selector to be used in the session establishment.
        /// </summary>
        /// <param name="compressionSelector">The compression selector.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public IEstablishedClientChannelBuilder WithCompression(Func<SessionCompression[], SessionCompression> compressionSelector)
        {
            if (compressionSelector == null)
            {
                throw new ArgumentNullException(nameof(compressionSelector));
            }

            CompressionSelector = compressionSelector;
            return this;
        }

        /// <summary>
        /// Sets the encryption option to be used in the session establishment.
        /// </summary>
        /// <param name="encryption">The encryption.</param>
        /// <returns></returns>
        public IEstablishedClientChannelBuilder WithEncryption(SessionEncryption encryption)
        {
            return WithEncryption(options => encryption);
        }

        /// <summary>
        /// Sets the encryption selector to be used in the session establishment.
        /// </summary>
        /// <param name="encryptionSelector">The encryption selector.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public IEstablishedClientChannelBuilder WithEncryption(Func<SessionEncryption[], SessionEncryption> encryptionSelector)
        {
            if (encryptionSelector == null)
            {
                throw new ArgumentNullException(nameof(encryptionSelector));
            }

            EncryptionSelector = encryptionSelector;
            return this;
        }

        /// <summary>
        /// Sets the authentication password to be used in the session establishment.
        /// </summary>
        /// <param name="password">The authentication password, in plain text.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public IEstablishedClientChannelBuilder WithPlainAuthentication(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

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
        public IEstablishedClientChannelBuilder WithKeyAuthentication(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var authentication = new KeyAuthentication();
            authentication.SetToBase64Key(key);
            return WithAuthentication(authentication);
        }

        public IEstablishedClientChannelBuilder WithTransportAuthentication(DomainRole domainRole)
        {
            var authentication = new TransportAuthentication();
            authentication.DomainRole = domainRole;
            return WithAuthentication(authentication);
        }

        public IEstablishedClientChannelBuilder WithExternalAuthentication(string token, string issuer)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            var authentication = new ExternalAuthentication
            {
                Token = token,
                Issuer = issuer
            };
            return WithAuthentication(authentication);
        }

        /// <summary>
        /// Sets the authentication to be used in the session establishment.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public IEstablishedClientChannelBuilder WithAuthentication<TAuthentication>() where TAuthentication : Authentication, new()
        {
            return WithAuthentication((schemes, roundtrip) => new TAuthentication());
        }

        /// <summary>
        /// Sets the authentication to be used in the session establishment.
        /// </summary>
        /// <param name="authentication">The authentication.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public IEstablishedClientChannelBuilder WithAuthentication(Authentication authentication)
        {
            if (authentication == null)
            {
                throw new ArgumentNullException(nameof(authentication));
            }

            return WithAuthentication((schemes, roundtrip) => authentication);
        }

        /// <summary>
        /// Sets the authentication to be used in the session establishment.
        /// </summary>
        /// <param name="authenticator">The authenticator.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public IEstablishedClientChannelBuilder WithAuthentication(Func<AuthenticationScheme[], Authentication, Authentication> authenticator)
        {
            if (authenticator == null)
            {
                throw new ArgumentNullException(nameof(authenticator));
            }

            Authenticator = authenticator;
            return this;
        }

        /// <summary>
        /// Sets the identity to be used in the session establishment.
        /// </summary>
        /// <param name="identity">The identity to be used.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public IEstablishedClientChannelBuilder WithIdentity(Identity identity)
        {
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            Identity = identity;
            return this;
        }

        /// <summary>
        /// Sets the instance name to be used in the session establishment.
        /// </summary>
        /// <param name="instance">The instance to be used.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public IEstablishedClientChannelBuilder WithInstance(string instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            Instance = instance;
            return this;
        }

        /// <summary>
        /// Adds a handler to be executed after the channel is built and established.
        /// </summary>
        /// <param name="establishedHandler">The handler to be executed.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public IEstablishedClientChannelBuilder AddEstablishedHandler(Func<IClientChannel, CancellationToken, Task> establishedHandler)
        {
            if (establishedHandler == null)
            {
                throw new ArgumentNullException(nameof(establishedHandler));
            }

            _establishedHandlers.Add(establishedHandler);
            return this;
        }

        /// <summary>
        /// Creates a copy of the current builder instance.
        /// </summary>
        /// <returns></returns>
        public IEstablishedClientChannelBuilder Copy()
        {
            return (IEstablishedClientChannelBuilder)MemberwiseClone();
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
                            CompressionSelector,
                            EncryptionSelector,
                            Identity,
                            Authenticator,
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

        internal EstablishedClientChannelBuilder ShallowCopy()
        {
            return (EstablishedClientChannelBuilder)MemberwiseClone();
        }
    }
}
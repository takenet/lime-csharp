using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Security;

namespace Lime.Protocol.Client
{
    public interface IEstablishedClientChannelBuilder
    {
        /// <summary>
        /// Gets the associated channel builder.
        /// </summary>
        IClientChannelBuilder ChannelBuilder { get; }

        /// <summary>
        /// Gets the identity.
        /// </summary>        
        Identity Identity { get; }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        string Instance { get; }

        /// <summary>
        /// Gets the establishment timeout
        /// </summary>
        TimeSpan EstablishmentTimeout { get; }

        /// <summary>
        /// Gets the compression selector.
        /// </summary>
        Func<SessionCompression[], SessionCompression> CompressionSelector { get; }

        /// <summary>
        /// Gets the encryption selector.
        /// </summary>
        Func<SessionEncryption[], SessionEncryption> EncryptionSelector { get; }

        /// <summary>
        /// Gets the authenticator.
        /// </summary>
        Func<AuthenticationScheme[], Authentication, Authentication> Authenticator { get; }

        /// <summary>
        /// Gets the established handlers.
        /// </summary>
        IEnumerable<Func<IClientChannel, CancellationToken, Task>> EstablishedHandlers { get; }

        /// <summary>
        /// Sets the timeout to Build and establish a new session
        /// </summary>
        /// <param name="timeout">The timeout</param>
        /// <returns></returns>
        IEstablishedClientChannelBuilder WithEstablishmentTimeout(TimeSpan timeout);

        /// <summary>
        /// Sets the compression option to be used in the session establishment.
        /// </summary>
        /// <param name="compression">The compression.</param>
        /// <returns></returns>
        IEstablishedClientChannelBuilder WithCompression(SessionCompression compression);

        /// <summary>
        /// Sets the compression selector to be used in the session establishment.
        /// </summary>
        /// <param name="compressionSelector">The compression selector.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        IEstablishedClientChannelBuilder WithCompression(Func<SessionCompression[], SessionCompression> compressionSelector);

        /// <summary>
        /// Sets the encryption option to be used in the session establishment.
        /// </summary>
        /// <param name="encryption">The encryption.</param>
        /// <returns></returns>
        IEstablishedClientChannelBuilder WithEncryption(SessionEncryption encryption);

        /// <summary>
        /// Sets the encryption selector to be used in the session establishment.
        /// </summary>
        /// <param name="encryptionSelector">The encryption selector.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        IEstablishedClientChannelBuilder WithEncryption(Func<SessionEncryption[], SessionEncryption> encryptionSelector);

        /// <summary>
        /// Sets the authentication password to be used in the session establishment.
        /// </summary>
        /// <param name="password">The authentication password, in plain text.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        IEstablishedClientChannelBuilder WithPlainAuthentication(string password);

        /// <summary>
        /// Sets the authentication key to be used in the session establishment.
        /// </summary>
        /// <param name="key">The authentication key.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        IEstablishedClientChannelBuilder WithKeyAuthentication(string key);

        /// <summary>
        /// Set the external authentication token to be used in the session establishment.
        /// </summary>
        /// <param name="token">The authentication token</param>
        /// <param name="issuer"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        IEstablishedClientChannelBuilder WithExternalAuthentication(string token, string issuer);

        /// <summary>
        /// Sets the authentication to be used in the session establishment.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        IEstablishedClientChannelBuilder WithAuthentication<TAuthentication>() where TAuthentication : Authentication, new();

        /// <summary>
        /// Sets the authentication to be used in the session establishment.
        /// </summary>
        /// <param name="authentication">The authentication.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        IEstablishedClientChannelBuilder WithAuthentication(Authentication authentication);

        /// <summary>
        /// Sets the authentication to be used in the session establishment.
        /// </summary>
        /// <param name="authenticator">The authenticator.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        IEstablishedClientChannelBuilder WithAuthentication(
            Func<AuthenticationScheme[], Authentication, Authentication> authenticator);

        /// <summary>
        /// Sets the identity to be used in the session establishment.
        /// </summary>
        /// <param name="identity">The identity to be used.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        IEstablishedClientChannelBuilder WithIdentity(Identity identity);

        /// <summary>
        /// Sets the instance name to be used in the session establishment.
        /// </summary>
        /// <param name="instance">The instance to be used.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        IEstablishedClientChannelBuilder WithInstance(string instance);

        /// <summary>
        /// Adds a handler to be executed after the channel is built and established.
        /// </summary>
        /// <param name="establishedHandler">The handler to be executed.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        IEstablishedClientChannelBuilder AddEstablishedHandler(Func<IClientChannel, CancellationToken, Task> establishedHandler);

        /// <summary>
        /// Creates a copy of the current builder instance.
        /// </summary>
        /// <returns></returns>
        IEstablishedClientChannelBuilder Copy();

        /// <summary>
        /// Builds a <see cref="ClientChannel"/> instance and establish the session using the builder options.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<IClientChannel> BuildAndEstablishAsync(CancellationToken cancellationToken);
    }
}
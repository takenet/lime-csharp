using System;
using System.Threading;
using System.Threading.Tasks;

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
        /// Builds a <see cref="ClientChannel"/> instance and establish the session using the builder options.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<IClientChannel> BuildAndEstablishAsync(CancellationToken cancellationToken);
    }
}
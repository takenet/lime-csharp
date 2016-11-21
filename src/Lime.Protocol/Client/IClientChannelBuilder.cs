using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Client
{
    public interface IClientChannelBuilder
    {
        /// <summary>
        /// Gets the server URI.
        /// </summary>
        Uri ServerUri { get; }

        /// <summary>
        /// Gets the channel send timeout.
        /// </summary>        
        TimeSpan SendTimeout { get; }

        /// <summary>
        /// Gets the channel consume timeout.
        /// </summary>        
        TimeSpan? ConsumeTimeout { get; }

        /// <summary>
        /// Gets the channel close timeout.
        /// </summary>        
        TimeSpan? CloseTimeout { get; }

        /// <summary>
        /// Gets the buffers limit.
        /// </summary>        
        int EnvelopeBufferSize { get; }

        /// <summary>
        /// Builds a <see cref="ClientChannel"/> instance connecting the transport.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<IClientChannel> BuildAsync(CancellationToken cancellationToken);
    }
}
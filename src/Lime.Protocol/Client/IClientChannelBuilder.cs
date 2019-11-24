using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;

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
        /// Gets the channel send batch flush interval.
        /// </summary>
        TimeSpan SendFlushBatchInterval { get; }

        /// <summary>
        /// Gets the batch size when sending to the channel.
        /// </summary>
        int SendBatchSize { get; }

        /// <summary>
        /// Gets the channel command processor.
        /// </summary>
        IChannelCommandProcessor ChannelCommandProcessor { get; }

        /// <summary>
        /// Sets the send timeout.
        /// </summary>
        /// <param name="sendTimeout">The send timeout.</param>
        /// <returns></returns>
        IClientChannelBuilder WithSendTimeout(TimeSpan sendTimeout);

        /// <summary>
        /// Sets the consume timeout.
        /// </summary>
        /// <param name="consumeTimeout">The consume timeout.</param>
        /// <returns></returns>
        IClientChannelBuilder WithConsumeTimeout(TimeSpan? consumeTimeout);

        /// <summary>
        /// Sets the close timeout.
        /// </summary>
        /// <param name="closeTimeout">The close timeout.</param>
        /// <returns></returns>
        IClientChannelBuilder WithCloseTimeout(TimeSpan? closeTimeout);

        /// <summary>
        /// Sets the envelope buffer size.
        /// </summary>
        /// <param name="envelopeBufferSize">The buffers limit.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        IClientChannelBuilder WithEnvelopeBufferSize(int envelopeBufferSize);

        /// <summary>
        /// Sets the channel send batch size.
        /// </summary>
        /// <param name="sendBatchSize"></param>
        /// <returns></returns>
        IClientChannelBuilder WithSendBatchSize(int sendBatchSize);

        /// <summary>
        /// Sets the channel send flush batch interval.
        /// </summary>
        /// <param name="sendFlushBatchInterval"></param>
        /// <returns></returns>
        IClientChannelBuilder WithSendFlushBatchInterval(TimeSpan sendFlushBatchInterval);

        /// <summary>
        /// Sets the channel command processor to be used.
        /// </summary>
        /// <param name="channelCommandProcessor">The channel command processor.</param>
        /// <returns></returns>
        IClientChannelBuilder WithChannelCommandProcessor(IChannelCommandProcessor channelCommandProcessor);

        /// <summary>
        /// Adds a message module to the channel.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns></returns>
        IClientChannelBuilder AddMessageModule(IChannelModule<Message> module);

        /// <summary>
        /// Adds a message module to the channel.
        /// </summary>
        /// <param name="moduleFactory">The module factory.</param>
        /// <returns></returns>
        IClientChannelBuilder AddMessageModule(Func<IClientChannel, IChannelModule<Message>> moduleFactory);

        /// <summary>
        /// Adds a notification module to the channel.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns></returns>
        IClientChannelBuilder AddNotificationModule(IChannelModule<Notification> module);

        /// <summary>
        /// Adds a notification module to the channel.
        /// </summary>
        /// <param name="moduleFactory">The module factory.</param>
        /// <returns></returns>
        IClientChannelBuilder AddNotificationModule(Func<IClientChannel, IChannelModule<Notification>> moduleFactory);

        /// <summary>
        /// Adds a command module to the channel.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns></returns>
        IClientChannelBuilder AddCommandModule(IChannelModule<Command> module);

        /// <summary>
        /// Adds a command module to the channel.
        /// </summary>
        /// <param name="moduleFactory">The module factory.</param>
        /// <returns></returns>
        IClientChannelBuilder AddCommandModule(Func<IClientChannel, IChannelModule<Command>> moduleFactory);

        /// <summary>
        /// Adds a handler to be executed after the channel is built.
        /// </summary>
        /// <param name="builtHandler">The handler to be executed.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        IClientChannelBuilder AddBuiltHandler(Func<IClientChannel, CancellationToken, Task> builtHandler);

        /// <summary>
        /// Builds a <see cref="ClientChannel"/> instance connecting the transport.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<IClientChannel> BuildAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Creates an <see cref="EstablishedClientChannelBuilder"/> to allow building and establishment of <see cref="ClientChannel"/> instances.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        IEstablishedClientChannelBuilder CreateEstablishedClientChannelBuilder();
    }
}
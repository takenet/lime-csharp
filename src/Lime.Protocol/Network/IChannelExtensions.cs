using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Utility extensions for the
    /// IChannel interface
    /// </summary>
    public static class IChannelExtensions
    {
        #region Private Fields

        private static SemaphoreSlim _processCommandSemaphore;

        #endregion

        #region Constructor

        static IChannelExtensions()
        {
            _processCommandSemaphore = new SemaphoreSlim(1);
        }

        #endregion

        /// <summary>
        /// Sends the envelope using the appropriate
        /// method for its type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="envelope"></param>
        /// <returns></returns>
        public static async Task SendAsync<T>(this IChannel channel, T envelope) where T : Envelope
        {
            if (channel == null)
            {
                throw new ArgumentNullException("channel");
            }

            if (typeof(T) == typeof(Notification))
            {
                await channel.SendNotificationAsync(envelope as Notification).ConfigureAwait(false);
            }
            else if (typeof(T) == typeof(Message))
            {
                await channel.SendMessageAsync(envelope as Message).ConfigureAwait(false);
            }
            else if (typeof(T) == typeof(Command))
            {
                await channel.SendCommandAsync(envelope as Command).ConfigureAwait(false);
            }
            else if (typeof(T) == typeof(Session))
            {
                await channel.SendSessionAsync(envelope as Session).ConfigureAwait(false);
            }
            else
            {
                throw new ArgumentException("Invalid or unknown envelope type");
            }
        }
        
        /// <summary>
        /// Composes a command envelope with a
        /// get method for the specified resource.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <param name="channel">The channel.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">channel</exception>
        /// <exception cref="LimeException">Returns an exception with the failure reason</exception>
        public static Task<TResource> GetResourceAsync<TResource>(this IChannel channel, CancellationToken cancellationToken) where TResource : Document, new()
        {
            return GetResourceAsync<TResource>(channel, new TResource(), cancellationToken);
        }

        /// <summary>
        /// Composes a command envelope with a
        /// get method for the specified resource.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <param name="channel">The channel.</param>
        /// <param name="from">From.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">channel</exception>
        /// <exception cref="LimeException">Returns an exception with the failure reason</exception>
        public static Task<TResource> GetResourceAsync<TResource>(this IChannel channel, Node from, CancellationToken cancellationToken) where TResource : Document, new()
        {
            return GetResourceAsync<TResource>(channel, new TResource(), from, cancellationToken);
        }

        /// <summary>
        /// Composes a command envelope with a
        /// get method for the specified resource.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <param name="channel">The channel.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">channel</exception>
        /// <exception cref="LimeException">Returns an exception with the failure reason</exception>
        public static Task<TResource> GetResourceAsync<TResource>(this IChannel channel, TResource resource, CancellationToken cancellationToken) where TResource : Document
        {
            return GetResourceAsync<TResource>(channel, resource, null, cancellationToken);
        }

        /// <summary>
        /// Composes a command envelope with a
        /// get method for the specified resource.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <param name="channel">The channel.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="from">From.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">channel</exception>
        /// <exception cref="LimeException">Returns an exception with the failure reason</exception>
        public static async Task<TResource> GetResourceAsync<TResource>(this IChannel channel, TResource resource, Node from, CancellationToken cancellationToken) where TResource : Document
        {
            if (channel == null)
            {
                throw new ArgumentNullException("channel");
            }

            if (resource == null)
            {
                throw new ArgumentNullException("resource");
            }

            var requestCommand = new Command()
            {
                From = from,
                Method = CommandMethod.Get,
                Resource = resource
            };

            var responseCommand = await ProcessCommandAsync(channel, requestCommand, cancellationToken).ConfigureAwait(false);
            if (responseCommand.Status == CommandStatus.Success)
            {
                return (TResource)responseCommand.Resource;
            }
            else
            {
                throw new LimeException(responseCommand.Reason);
            }
        }

        /// <summary>
        /// Sets the resource value asynchronous.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <param name="channel">The channel.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">channel</exception>
        public static Task SetResourceAsync<TResource>(this IChannel channel, TResource resource, CancellationToken cancellationToken) where TResource : Document
        {
            return SetResourceAsync(channel, resource, null, cancellationToken);            
        }

        /// <summary>
        /// Sets the resource value asynchronous.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <param name="channel">The channel.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="from">From.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">channel</exception>
        /// <exception cref="LimeException"></exception>
        public static async Task SetResourceAsync<TResource>(this IChannel channel, TResource resource, Node from, CancellationToken cancellationToken) where TResource : Document
        {
            if (channel == null)
            {
                throw new ArgumentNullException("channel");
            }

            if (resource == null)
            {
                throw new ArgumentNullException("resource");
            }

            var requestCommand = new Command()
            {
                From = from,
                Method = CommandMethod.Set,
                Resource = resource
            };

            var responseCommand = await ProcessCommandAsync(channel, requestCommand, cancellationToken).ConfigureAwait(false);
            if (responseCommand.Status != CommandStatus.Success)
            {
                throw new LimeException(responseCommand.Reason);
            }
        }


        /// <summary>
        /// Composes a command envelope with a
        /// delete method for the specified resource.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <param name="channel">The channel.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">channel</exception>
        /// <exception cref="LimeException">Returns an exception with the failure reason</exception>
        public static Task<TResource> DeleteResourceAsync<TResource>(this IChannel channel, CancellationToken cancellationToken) where TResource : Document, new()
        {
            return DeleteResourceAsync<TResource>(channel, new TResource(), null, cancellationToken);
        }

        /// <summary>
        /// Composes a command envelope with a
        /// delete method for the specified resource.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <param name="channel">The channel.</param>
        /// <param name="from">From.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">channel</exception>
        /// <exception cref="LimeException">Returns an exception with the failure reason</exception>
        public static Task<TResource> DeleteResourceAsync<TResource>(this IChannel channel, Node from, CancellationToken cancellationToken) where TResource : Document, new()
        {
            return DeleteResourceAsync<TResource>(channel, new TResource(), from, cancellationToken);
        }

        /// <summary>
        /// Composes a command envelope with a
        /// delete method for the specified resource.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <param name="channel">The channel.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">channel</exception>
        /// <exception cref="LimeException">Returns an exception with the failure reason</exception>
        public static Task<TResource> DeleteResourceAsync<TResource>(this IChannel channel, TResource resource, CancellationToken cancellationToken) where TResource : Document
        {
            return DeleteResourceAsync<TResource>(channel, resource, null, cancellationToken);
        }

        /// <summary>
        /// Composes a command envelope with a
        /// delete method for the specified resource.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <param name="channel">The channel.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="from">From.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">channel</exception>
        /// <exception cref="LimeException">Returns an exception with the failure reason</exception>
        public static async Task<TResource> DeleteResourceAsync<TResource>(this IChannel channel, TResource resource, Node from, CancellationToken cancellationToken) where TResource : Document
        {
            if (channel == null)
            {
                throw new ArgumentNullException("channel");
            }

            if (resource == null)
            {
                throw new ArgumentNullException("resource");
            }

            var requestCommand = new Command()
            {
                From = from,
                Method = CommandMethod.Delete,
                Resource = resource
            };

            var responseCommand = await ProcessCommandAsync(channel, requestCommand, cancellationToken).ConfigureAwait(false);
            if (responseCommand.Status == CommandStatus.Success)
            {
                return (TResource)responseCommand.Resource;
            }
            else
            {
                throw new LimeException(responseCommand.Reason);
            }
        }

        /// <summary>
        /// Sends a command request through the 
        /// channel and awaits for the response.
        /// This method synchronizes the channel
        /// calls to avoid multiple command processing
        /// conflicts. 
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="requestCommand">The command request.</param>
        /// <returns></returns>
        public static async Task<Command> ProcessCommandAsync(this IChannel channel, Command requestCommand, CancellationToken cancellationToken)
        {
            if (channel == null)
            {
                throw new ArgumentNullException("channel");
            }

            await _processCommandSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                await channel.SendCommandAsync(requestCommand).ConfigureAwait(false);
                return await channel.ReceiveCommandAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _processCommandSemaphore.Release();
            }
        }
    }
}

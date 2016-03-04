using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Utility extensions for the IChannel interface.
    /// </summary>
    public static class ChannelExtensions
    {
        /// <summary>
        /// Sends the envelope using the appropriate method for its type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="envelope"></param>
        /// <returns></returns>
        public static Task SendAsync<T>(this IEstablishedSenderChannel channel, T envelope) where T : Envelope, new()
        {
            return SendAsync(channel, envelope, CancellationToken.None);
        }

        /// <summary>
        /// Sends the envelope using the appropriate method for its type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">The channel.</param>
        /// <param name="envelope">The envelope.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        /// <exception cref="System.ArgumentException">Invalid or unknown envelope type</exception>
        public static async Task SendAsync<T>(this IEstablishedSenderChannel channel, T envelope, CancellationToken cancellationToken) where T : Envelope, new()
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));

            if (typeof(T) == typeof(Notification))
            {
                await channel.SendNotificationAsync(envelope as Notification, cancellationToken).ConfigureAwait(false);
            }
            else if (typeof(T) == typeof(Message))
            {
                await channel.SendMessageAsync(envelope as Message, cancellationToken).ConfigureAwait(false);
            }
            else if (typeof(T) == typeof(Command))
            {
                await channel.SendCommandAsync(envelope as Command, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw new ArgumentException("Invalid or unknown envelope type");
            }
        }

        /// <summary>
        /// Composes a command envelope with a get method for the specified resource.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        
        /// <param name="uri">The resource uri.</param>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">channel</exception>
        /// <exception cref="LimeException">Returns an exception with the failure reason</exception>
        public static Task<TResource> GetResourceAsync<TResource>(this ICommandChannel channel, LimeUri uri, CancellationToken cancellationToken) where TResource : Document, new()
        {
            return GetResourceAsync<TResource>(channel, uri, null, cancellationToken);
        }

        /// <summary>
        /// Composes a command envelope with a get method for the specified resource.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <param name="channel">The channel.</param>
        /// <param name="uri">The resource uri.</param>
        /// <param name="from">The originator to be used in the command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>        
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">channel</exception>
        /// <exception cref="LimeException">Returns an exception with the failure reason</exception>
        public static async Task<TResource> GetResourceAsync<TResource>(this ICommandChannel channel, LimeUri uri, Node from, CancellationToken cancellationToken) where TResource : Document
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (uri == null) throw new ArgumentNullException(nameof(uri));

            var requestCommand = new Command
            {
                From = from,
                Method = CommandMethod.Get,
                Uri = uri
            };

            var responseCommand = await channel.ProcessCommandAsync(requestCommand, cancellationToken).ConfigureAwait(false);
            if (responseCommand.Status == CommandStatus.Success)
            {
                return (TResource)responseCommand.Resource;
            }
            else if (responseCommand.Reason != null)
            {
                throw new LimeException(responseCommand.Reason.Code, responseCommand.Reason.Description);
            }
            else
            {
                throw new InvalidOperationException("An invalid command response was received");
            }
        }

        /// <summary>
        /// Sets the resource value asynchronous.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <param name="channel">The channel.</param>
        /// <param name="uri">The resource uri.</param>
        /// <param name="resource">The resource to be set.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">channel</exception>
        public static Task SetResourceAsync<TResource>(this ICommandChannel channel, LimeUri uri, TResource resource, CancellationToken cancellationToken) where TResource : Document
        {
            return SetResourceAsync(channel, uri, resource, null, cancellationToken);
        }

        /// <summary>
        /// Sets the resource value asynchronous.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <param name="channel">The channel.</param>
        /// <param name="uri">The resource uri.</param>
        /// <param name="resource">The resource to be set.</param>
        /// <param name="from">The originator to be used in the command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">channel</exception>
        /// <exception cref="LimeException"></exception>
        public static async Task SetResourceAsync<TResource>(this ICommandChannel channel, LimeUri uri, TResource resource, Node from, CancellationToken cancellationToken) where TResource : Document
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (resource == null) throw new ArgumentNullException(nameof(resource));

            var requestCommand = new Command
            {
                From = from,
                Method = CommandMethod.Set,
                Uri = uri,
                Resource = resource
            };

            var responseCommand = await channel.ProcessCommandAsync(requestCommand, cancellationToken).ConfigureAwait(false);
            if (responseCommand.Status != CommandStatus.Success)
            {
                if (responseCommand.Reason != null)
                {
                    throw new LimeException(responseCommand.Reason.Code, responseCommand.Reason.Description);
                }
                else
                {
#if DEBUG
                    if (requestCommand == responseCommand)
                    {
                        throw new InvalidOperationException("The request and the response are the same instance");
                    }
#endif

                    throw new InvalidOperationException("An invalid command response was received");
                }
            }
        }

        /// <summary>
        /// Composes a command envelope with a
        /// delete method for the specified resource.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="uri">The resource uri.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">channel</exception>
        /// <exception cref="LimeException">Returns an exception with the failure reason</exception>
        public static Task DeleteResourceAsync(this ICommandChannel channel, LimeUri uri, CancellationToken cancellationToken)
        {
            return DeleteResourceAsync(channel, uri, null, cancellationToken);
        }

        /// <summary>
        /// Composes a command envelope with a
        /// delete method for the specified resource.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="uri">The resource uri.</param>
        /// <param name="from">The originator to be used in the command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">channel</exception>
        /// <exception cref="LimeException">Returns an exception with the failure reason</exception>
        public static async Task DeleteResourceAsync(this ICommandChannel channel, LimeUri uri, Node from, CancellationToken cancellationToken)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (uri == null) throw new ArgumentNullException(nameof(uri));

            var requestCommand = new Command
            {
                From = from,
                Method = CommandMethod.Delete,
                Uri = uri
            };

            var responseCommand = await channel.ProcessCommandAsync(requestCommand, cancellationToken).ConfigureAwait(false);
            if (responseCommand.Status != CommandStatus.Success)
            {
                if (responseCommand.Reason != null)
                {
                    throw new LimeException(responseCommand.Reason.Code, responseCommand.Reason.Description);
                }
                throw new InvalidOperationException("An invalid command response was received");
            }
        }
    }
}
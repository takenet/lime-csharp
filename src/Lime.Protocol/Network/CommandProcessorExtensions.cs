using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Tracing;

namespace Lime.Protocol.Network
{
    public static class CommandProcessorExtensions
    {
        /// <summary>
        /// Processes a command throwing a <see cref="LimeException"/> in case of <see cref="CommandStatus.Failure"/> status.
        /// </summary>
        public static Task<Command> ProcessCommandOrThrowAsync(
            this ICommandProcessor channel,
            CommandMethod method,
            LimeUri uri,
            CancellationToken cancellationToken,
            Node from = null,
            Node to = null,
            Node pp = null,
            IDictionary<string, string> metadata = null)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));

            var command = new Command()
            {
                From = from,
                To = to,
                Pp = pp,
                Method = method,
                Uri = uri,
                Metadata = metadata
            };

            Activity.Current?.InjectTraceParentIfAbsent(command);

            return ProcessCommandOrThrowAsync(
                channel,
                command,
                cancellationToken);
        }
        
        /// <summary>
        /// Processes a command throwing a <see cref="LimeException"/> in case of <see cref="CommandStatus.Failure"/> status.
        /// </summary>
        public static Task<Command> ProcessCommandOrThrowAsync<TRequestResource>(
            this ICommandProcessor channel,
            CommandMethod method,
            LimeUri uri,
            TRequestResource resource,
            CancellationToken cancellationToken,
            Node from = null,
            Node to = null,
            Node pp = null,
            IDictionary<string, string> metadata = null)
            where TRequestResource : Document
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (resource == null) throw new ArgumentNullException(nameof(resource));

            var command = new Command()
            {
                From = from,
                To = to,
                Pp = pp,
                Method = method,
                Uri = uri,
                Resource = resource,
                Metadata = metadata
            };

            Activity.Current?.InjectTraceParentIfAbsent(command);

            return ProcessCommandOrThrowAsync(
                channel,
                command,
                cancellationToken);
        }
                
        /// <summary>
        /// Processes a command throwing a <see cref="LimeException"/> in case of <see cref="CommandStatus.Failure"/> status.
        /// </summary>
        public static async Task<Command> ProcessCommandOrThrowAsync(
            this ICommandProcessor channel,
            Command requestCommand,
            CancellationToken cancellationToken)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (requestCommand == null) throw new ArgumentNullException(nameof(requestCommand));
            
            var responseCommand = await channel.ProcessCommandAsync(requestCommand, cancellationToken).ConfigureAwait(false);
            if (responseCommand.Status != CommandStatus.Success)
            {
                if (responseCommand.Reason != null)
                {
                    throw new LimeException(responseCommand.Reason.Code, responseCommand.Reason.Description);
                }

                throw new InvalidOperationException("An invalid command response was received");
            }

            return responseCommand;
        }
        
        /// <summary>
        /// Processes a command throwing a <see cref="LimeException"/> in case of <see cref="CommandStatus.Failure"/> status and returns the response resource.
        /// </summary>
        public static Task<TResponseResource> ProcessCommandWithResponseResourceAsync<TResponseResource>(
            this ICommandProcessor channel,
            CommandMethod method,
            LimeUri uri,
            CancellationToken cancellationToken,
            Node from = null,
            Node to = null,
            Node pp = null,
            IDictionary<string, string> metadata = null)
            where TResponseResource : Document
        {
            var command = new Command()
            {
                From = from,
                To = to,
                Pp = pp,
                Method = method,
                Uri = uri,
                Metadata = metadata
            };

            Activity.Current?.InjectTraceParentIfAbsent(command);

            return ProcessCommandWithResponseResourceAsync<TResponseResource>(
                    channel,
                    command,
                    cancellationToken);
        }
        
        /// <summary>
        /// Processes a command throwing a <see cref="LimeException"/> in case of <see cref="CommandStatus.Failure"/> status and returns the response resource.
        /// </summary>
        public static Task<TResponseResource> ProcessCommandWithResponseResourceAsync<TRequestResource, TResponseResource>(
            this ICommandProcessor channel,
            CommandMethod method,
            LimeUri uri,
            TRequestResource resource,
            CancellationToken cancellationToken,
            Node from = null,
            Node to = null,
            Node pp = null,
            IDictionary<string, string> metadata = null)
            where TRequestResource : Document
            where TResponseResource : Document
        {
            var command = new Command()
            {
                From = from,
                To = to,
                Pp = pp,
                Method = method,
                Uri = uri,
                Resource = resource,
                Metadata = metadata
            };

            Activity.Current?.InjectTraceParentIfAbsent(command);

            return ProcessCommandWithResponseResourceAsync<TResponseResource>(
                channel,
                command,
                cancellationToken);
        }        
        
        /// <summary>
        /// Processes a command throwing a <see cref="LimeException"/> in case of <see cref="CommandStatus.Failure"/> status and returns the response resource.
        /// </summary>
        public static async Task<TResponseResource> ProcessCommandWithResponseResourceAsync<TResponseResource>(
            this ICommandProcessor channel,
            Command requestCommand,
            CancellationToken cancellationToken)
            where TResponseResource : Document
        {
            var responseCommand = await ProcessCommandOrThrowAsync(channel, requestCommand, cancellationToken).ConfigureAwait(false);
            return (TResponseResource)responseCommand.Resource;
        }
        
        /// <summary>
        /// Gets a resource from the specified URI.
        /// </summary>
        public static Task<TResource> GetResourceAsync<TResource>(
            this ICommandProcessor channel,
            LimeUri uri,
            CancellationToken cancellationToken,
            Node from = null,
            Node to = null,
            Node pp = null,
            IDictionary<string, string> metadata = null) 
            where TResource : Document
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (uri == null) throw new ArgumentNullException(nameof(uri));

            return ProcessCommandWithResponseResourceAsync<TResource>(
                channel,
                CommandMethod.Get,
                uri,
                cancellationToken,
                from,
                to,
                pp,
                metadata);
        }
        
        /// <summary>
        /// Sets a the resource value in the specified URI.
        /// </summary>
        public static Task SetResourceAsync<TRequestResource>(
            this ICommandProcessor channel,
            LimeUri uri,
            TRequestResource resource,
            CancellationToken cancellationToken,
            Node from = null,
            Node to = null,
            Node pp = null,
            IDictionary<string, string> metadata = null) 
            where TRequestResource : Document
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (resource == null) throw new ArgumentNullException(nameof(resource));

            return ProcessCommandOrThrowAsync(
                channel,
                CommandMethod.Set,
                uri,
                resource,
                cancellationToken,
                from,
                to,
                pp,
                metadata);
        }

        /// <summary>
        /// Sets a the resource value in the specified URI returning the response command resource value.
        /// </summary>
        public static Task<TResponseResource> SetResourceAsync<TRequestResource, TResponseResource>(
            this ICommandProcessor channel,
            LimeUri uri,
            TRequestResource resource,
            CancellationToken cancellationToken,
            Node from = null,
            Node to = null,
            Node pp = null,
            IDictionary<string, string> metadata = null)
            where TRequestResource : Document
            where TResponseResource : Document
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (resource == null) throw new ArgumentNullException(nameof(resource));

            return ProcessCommandWithResponseResourceAsync<TRequestResource, TResponseResource>(
                channel,
                CommandMethod.Set,
                uri,
                resource,
                cancellationToken,
                from,
                to,
                pp,
                metadata);
        }

        /// <summary>
        /// Delete a resource in the specified URI.
        /// </summary>
        public static Task DeleteResourceAsync(
            this ICommandProcessor channel,
            LimeUri uri,
            CancellationToken cancellationToken,
            Node from = null,
            Node to = null,
            Node pp = null,
            IDictionary<string, string> metadata = null)            
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (uri == null) throw new ArgumentNullException(nameof(uri));

            return ProcessCommandOrThrowAsync(channel, CommandMethod.Delete, uri, cancellationToken, from, to, pp, metadata);
        }
        
        /// <summary>
        /// Delete a resource in the specified URI returning the response command resource value.
        /// </summary>
        public static Task DeleteResourceAsync<TResponseResource>(
            this ICommandProcessor channel,
            LimeUri uri,
            CancellationToken cancellationToken,
            Node from = null,
            Node to = null,
            Node pp = null,
            IDictionary<string, string> metadata = null)
            where TResponseResource : Document
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (uri == null) throw new ArgumentNullException(nameof(uri));

            return ProcessCommandWithResponseResourceAsync<TResponseResource>(channel, CommandMethod.Delete, uri, cancellationToken, from, to, pp, metadata);
        }
        
        /// <summary>
        /// Merges a the resource value in the specified URI.
        /// </summary>
        public static Task MergeResourceAsync<TRequestResource>(
            this ICommandProcessor channel,
            LimeUri uri,
            TRequestResource resource,
            CancellationToken cancellationToken,
            Node from = null,
            Node to = null,
            Node pp = null,
            IDictionary<string, string> metadata = null) 
            where TRequestResource : Document
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (resource == null) throw new ArgumentNullException(nameof(resource));

            return ProcessCommandOrThrowAsync(
                channel,
                CommandMethod.Merge,
                uri,
                resource,
                cancellationToken,
                from,
                to,
                pp,
                metadata);
        }

        /// <summary>
        /// Merges a the resource value in the specified URI returning the response command resource value.
        /// </summary>
        public static Task<TResponseResource> MergeResourceAsync<TRequestResource, TResponseResource>(
            this ICommandProcessor channel,
            LimeUri uri,
            TRequestResource resource,
            CancellationToken cancellationToken,
            Node from = null,
            Node to = null,
            Node pp = null,
            IDictionary<string, string> metadata = null)
            where TRequestResource : Document
            where TResponseResource : Document
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (resource == null) throw new ArgumentNullException(nameof(resource));

            return ProcessCommandWithResponseResourceAsync<TRequestResource, TResponseResource>(
                channel,
                CommandMethod.Merge,
                uri,
                resource,
                cancellationToken,
                from,
                to,
                pp,
                metadata);
        }

        /// <summary>
        /// Subscribe a resource in the specified URI.
        /// </summary>
        public static Task SubscribeResourceAsync(
            this ICommandProcessor channel,
            LimeUri uri,
            CancellationToken cancellationToken,
            Node from = null,
            Node to = null,
            Node pp = null,
            IDictionary<string, string> metadata = null)            
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (uri == null) throw new ArgumentNullException(nameof(uri));

            return ProcessCommandOrThrowAsync(channel, CommandMethod.Subscribe, uri, cancellationToken, from, to, pp, metadata);
        }
        
        /// <summary>
        /// Subscribe a resource in the specified URI returning the response command resource value.
        /// </summary>
        public static Task SubscribeResourceAsync<TResponseResource>(
            this ICommandProcessor channel,
            LimeUri uri,
            CancellationToken cancellationToken,
            Node from = null,
            Node to = null,
            Node pp = null,
            IDictionary<string, string> metadata = null)
            where TResponseResource : Document
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (uri == null) throw new ArgumentNullException(nameof(uri));

            return ProcessCommandWithResponseResourceAsync<TResponseResource>(channel, CommandMethod.Subscribe, uri, cancellationToken, from, to, pp, metadata);
        }
                
        /// <summary>
        /// Unsubscribe a resource in the specified URI.
        /// </summary>
        public static Task UnsubscribeResourceAsync(
            this ICommandProcessor channel,
            LimeUri uri,
            CancellationToken cancellationToken,
            Node from = null,
            Node to = null,
            Node pp = null,
            IDictionary<string, string> metadata = null)            
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (uri == null) throw new ArgumentNullException(nameof(uri));

            return ProcessCommandOrThrowAsync(channel, CommandMethod.Unsubscribe, uri, cancellationToken, from, to, pp, metadata);
        }
        
        /// <summary>
        /// Unsubscribe a resource in the specified URI returning the response command resource value.
        /// </summary>
        public static Task UnsubscribeResourceAsync<TResponseResource>(
            this ICommandProcessor channel,
            LimeUri uri,
            CancellationToken cancellationToken,
            Node from = null,
            Node to = null,
            Node pp = null,
            IDictionary<string, string> metadata = null)
            where TResponseResource : Document
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (uri == null) throw new ArgumentNullException(nameof(uri));

            return ProcessCommandWithResponseResourceAsync<TResponseResource>(channel, CommandMethod.Unsubscribe, uri, cancellationToken, from, to, pp, metadata);
        }                       
        
        /// <summary>
        /// Observes a the resource value in the specified URI.
        /// </summary>
        public static Task ObserveResourceAsync<TRequestResource>(
            this ICommandProcessor channel,
            LimeUri uri,
            TRequestResource resource,
            CancellationToken cancellationToken,
            Node from = null,
            Node to = null,
            Node pp = null,
            IDictionary<string, string> metadata = null) 
            where TRequestResource : Document
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (resource == null) throw new ArgumentNullException(nameof(resource));

            return ProcessCommandOrThrowAsync(
                channel,
                CommandMethod.Observe,
                uri,
                resource,
                cancellationToken,
                from,
                to,
                pp,
                metadata);
        }

        /// <summary>
        /// Observes a the resource value in the specified URI returning the response command resource value.
        /// </summary>
        public static Task<TResponseResource> ObserveResourceAsync<TRequestResource, TResponseResource>(
            this ICommandProcessor channel,
            LimeUri uri,
            TRequestResource resource,
            CancellationToken cancellationToken,
            Node from = null,
            Node to = null,
            Node pp = null,
            IDictionary<string, string> metadata = null)
            where TRequestResource : Document
            where TResponseResource : Document
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (resource == null) throw new ArgumentNullException(nameof(resource));

            return ProcessCommandWithResponseResourceAsync<TRequestResource, TResponseResource>(
                channel,
                CommandMethod.Observe,
                uri,
                resource,
                cancellationToken,
                from,
                to,
                pp,
                metadata);
        }
    }
}
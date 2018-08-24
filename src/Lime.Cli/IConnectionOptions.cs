using Lime.Messaging.Resources;
using Lime.Protocol;
using System;
using System.Linq;

namespace Lime.Cli
{
    public interface IConnectionOptions
    {        
        string Identity { get; }
        
        string Password { get; }
        
        string AccessKey { get; }
        
        string Instance { get; }
        
        Uri ServerUri { get; }
        
        string PresenceStatus { get; }
        
        string PresenceRoutingRule { get; }

        string ReceiptEvents { get; }
    }

    public static class ConnectionOptionExtensions
    {
        public static ConnectionInformation ToConnectionInformation(this IConnectionOptions options)
        {
            return new ConnectionInformation()
            {
                Identity = options.Identity,
                Password = options.Password,
                ServerUri = options.ServerUri,
                Instance = options.Instance ?? $"{Environment.MachineName.ToLowerInvariant()}-cli",
                Presence = new Presence
                {
                    Status = options.PresenceStatus != null
                        ? Enum.Parse<PresenceStatus>(options.PresenceStatus, true)
                        : PresenceStatus.Available,
                    RoutingRule = options.PresenceRoutingRule != null
                        ? Enum.Parse<RoutingRule>(options.PresenceRoutingRule, true)
                        : RoutingRule.Instance,
                },
                Receipt = new Receipt()
                {
                    Events = options.ReceiptEvents != null 
                        ? options.ReceiptEvents.Split(',').Select(e => Enum.Parse<Event>(e, true)).ToArray()
                        : new[] { Event.Accepted, Event.Received, Event.Consumed }
                }
            };
        }
    }
}
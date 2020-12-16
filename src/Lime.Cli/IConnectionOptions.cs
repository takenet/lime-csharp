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
        
        string Key { get; }

        string Token { get; }

        string Issuer { get; }

        string Instance { get; }
        
        Uri Uri { get; }
        
        string PresenceStatus { get; }
        
        string PresenceRoutingRule { get; }

        string ReceiptEvents { get; }
    }

    public static class ConnectionOptionExtensions
    {
        public static ConnectionInformation ToConnectionInformation(this IConnectionOptions options)
        {
            Presence presence = null;
            if (options.PresenceStatus != null)
            {
                presence = new Presence()
                {
                    Status = Enum.Parse<PresenceStatus>(options.PresenceStatus, true),
                    RoutingRule = options.PresenceRoutingRule != null
                        ? Enum.Parse<RoutingRule>(options.PresenceRoutingRule, true)
                        : RoutingRule.Instance,
                };
            }

            Receipt receipt = null;
            if (options.ReceiptEvents != null)
            {
                receipt = new Receipt()
                {
                    Events = options
                        .ReceiptEvents
                        .Split(',')
                        .Select(e => Enum.Parse<Event>(e, true))
                        .ToArray()
                };
            }

            return new ConnectionInformation()
            {
                Identity = options.Identity,
                Password = options.Password,
                Key = options.Key,
                Token = options.Token,
                Issuer = options.Issuer,
                ServerUri = options.Uri,
                Instance = options.Instance ?? $"{Environment.MachineName.ToLowerInvariant()}-cli",
                Presence = presence,
                Receipt = receipt
            };
        }
    }
}
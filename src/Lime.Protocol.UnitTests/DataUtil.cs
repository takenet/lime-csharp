using Lime.Protocol.Contents;
using Lime.Protocol.Resources;
using Lime.Protocol.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.UnitTests
{
    public class DataUtil
    {
        private static Random _random = new Random();
        private static string _chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        public static int CreateRandomInt(int maxValue)
        {
            return _random.Next(maxValue);
        }

        public static string CreateRandomString(int size)
        {
            return new string(
                Enumerable.Repeat(_chars, size)
                          .Select(s => s[_random.Next(s.Length)])
                          .ToArray());
        }

        public static string CreateDomainName()
        {
            return string.Format("{0}.com", CreateRandomString(10));
        }

        public static string CreateSubdomainName()
        {
            return CreateRandomString(10);
        }

        public static string CreateInstanceName()
        {
            return CreateRandomString(5);
        }


        public static Identity CreateIdentity()
        {
            return new Identity()
            {
                Name = CreateRandomString(8),
                Domain = CreateDomainName()
            };
        }

        public static Node CreateNode()
        {
            var identity = CreateIdentity();

            return new Node()
            {
                Name = identity.Name,
                Domain = identity.Domain,
                Instance = CreateInstanceName()
            };
        }

        public static Session CreateSession()
        {
            return new Session()
            {
                Id = Guid.NewGuid(),
                From = CreateNode(),
                To = CreateNode()
            };
        }

        public static Reason CreateReason()
        {
            return new Reason()
            {
                Code = CreateRandomInt(100),
                Description = CreateRandomString(100)
            };
        }

        public static Authentication CreateAuthentication(AuthenticationScheme scheme)
        {
            switch (scheme)
            {
                case AuthenticationScheme.Guest:
                    return CreateGuestAuthentication();
                case AuthenticationScheme.Plain:
                    return CreatePlainAuthentication();
                default:
                    throw new ArgumentException("Unknown scheme");
            }

        }


        public static GuestAuthentication CreateGuestAuthentication()
        {
            return new GuestAuthentication();
        }

        public static PlainAuthentication CreatePlainAuthentication()
        {
            var authentication = new PlainAuthentication();
            authentication.SetToBase64Password(CreateRandomString(8));
            return authentication;
        }

        public static AuthenticationScheme[] CreateSchemeOptions()
        {
            return new AuthenticationScheme[] { AuthenticationScheme.Guest, AuthenticationScheme.Plain };
        }


        public static Message CreateMessage(Document content)
        {
            return new Message()
            {
                From = CreateNode(),
                To = CreateNode(),
                Content = content
            };
        }

        public static TextContent CreateTextContent()
        {
            return new TextContent()
            {
                Text = CreateRandomString(150)
            };
        }

        public static Notification CreateNotification(Event @event)
        {
            return new Notification()
            {
                From = CreateNode(),
                To = CreateNode(),
                Event = @event
            };
        }

        public static Command CreateCommand(Document resource = null, CommandMethod method = CommandMethod.Get, CommandStatus status = CommandStatus.Pending)
        {
            return new Command()
            {
                From = CreateNode(),
                To = CreateNode(),
                Method = method,
                Status = status,
                Resource = resource
            };
        }

        public static Ping CreatePing()
        {
            return new Ping();
        }

        public static Presence CreatePresence()
        {
            return new Presence()
            {
                Message = CreateRandomString(50),
                Priority = 1,
                RoutingRule = RoutingRule.IdentityByPriority,
                Status = PresenceStatus.Available
            };
        }


        public static MediaType CreateMediaType()
        {
            return new MediaType(
                "application",
                CreateRandomString(10),
                "json"
                );

        }

        public static Capability CreateCapability()
        {           
            return new Capability()
            {
                ContentTypes = new[] 
                { 
                    CreateMediaType(),
                    CreateMediaType(),
                    CreateMediaType()
                },
                ResourceTypes = new[] 
                { 
                    CreateMediaType(),
                    CreateMediaType(),
                    CreateMediaType()
                }
            };
        }

        public static Exception CreateException()
        {
            return new Exception(CreateRandomString(50));
        }
    }
}

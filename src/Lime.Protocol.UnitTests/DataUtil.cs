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
            return "limeprotocol.org";
        }

        public static string CreateSubdomainName()
        {
            return "application";
        }

        public static string CreateInstanceName()
        {
            return "home";
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
                    return new GuestAuthentication();
                case AuthenticationScheme.Plain:
                    var authentication = new PlainAuthentication();
                    authentication.SetToBase64Password(CreateRandomString(8));
                    return authentication;
                default:
                    throw new ArgumentException("Unknown scheme");
            }

        }

        public static Message CreateMessage()
        {
            return new Message()
            {
                From = CreateNode(),
                To = CreateNode()
            };
        }

        public static TextContent CreateTextContent()
        {
            return new TextContent()
            {
                Text = CreateRandomString(150)
            };
        }

        public static Notification CreateNotification()
        {
            return new Notification()
            {
                From = CreateNode(),
                To = CreateNode()
            };
        }

        public static Command CreateCommand(Document resource, CommandMethod method = CommandMethod.Get, CommandStatus status = CommandStatus.Pending)
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
    }
}

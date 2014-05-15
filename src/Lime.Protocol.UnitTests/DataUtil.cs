using Lime.Protocol.Contents;
using Lime.Protocol.Resources;
using Lime.Protocol.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        public static string CreateMessageJson()
        {
            var id = Guid.NewGuid();
            var from = DataUtil.CreateNode();
            var pp = DataUtil.CreateNode();
            var to = DataUtil.CreateNode();

            string randomKey1 = "randomString1";
            string randomKey2 = "randomString2";
            string randomString1 = DataUtil.CreateRandomString(DataUtil.CreateRandomInt(50));
            string randomString2 = DataUtil.CreateRandomString(DataUtil.CreateRandomInt(50));

            var text = DataUtil.CreateRandomString(DataUtil.CreateRandomInt(50));

            return string.Format(
                "{{\"type\":\"application/vnd.lime.text+json\",\"content\":{{\"text\":\"{0}\"}},\"id\":\"{1}\",\"from\":\"{2}\",\"pp\":\"{3}\",\"to\":\"{4}\",\"metadata\":{{\"{5}\":\"{6}\",\"{7}\":\"{8}\"}}}}",
                text,
                id,
                from,
                pp,
                to,
                randomKey1,
                randomString1,
                randomKey2,
                randomString2
                );
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

        public static Session CreateSession(SessionState state = SessionState.New)
        {
            return new Session()
            {
                Id = Guid.NewGuid(),
                From = CreateNode(),
                To = CreateNode(),
                State = state
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

        public static CancellationToken CreateCancellationToken()
        {
            return CancellationToken.None;
        }

        public static CancellationTokenSource CreateCancellationTokenSource()
        {
            return new CancellationTokenSource();
        }

        public static CancellationToken CreateCancellationToken(TimeSpan timeout)
        {
            var cts = CreateCancellationTokenSource();
            cts.CancelAfter(timeout);
            return cts.Token;
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

        public static JsonDocument CreateJsonDocument()
        {
            return new JsonDocument(                
                new Dictionary<string, object>
                {
                    { CreateRandomString(10), CreateRandomString(50) },
                    { CreateRandomString(10), CreateRandomInt(50) }
                },
                CreateMediaType());
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

        public static Roster CreateRoster()
        {
            return new Roster()
            {
                Contacts = new[]
                {
                    new Contact()
                    {
                        Identity = CreateIdentity(),
                        Name = CreateRandomString(50),
                        IsPending = true,
                        ShareAccountInfo = false,
                        SharePresence = true
                    },
                    new Contact()
                    {
                        Identity = CreateIdentity(),
                        Name = CreateRandomString(50),
                        IsPending = false,
                        ShareAccountInfo = true,
                        SharePresence = false
                    },
                    new Contact()
                    {
                        Identity = CreateIdentity(),
                        Name = CreateRandomString(50),
                        IsPending = true,
                        ShareAccountInfo = true,
                        SharePresence = false
                    },
                }

            };
        }

        public static Exception CreateException()
        {
            return new Exception(CreateRandomString(50));
        }

        public static T CreateException<T>() where T : Exception, new()
        {
            return new T();
        }

        public static Uri CreateUri(string scheme = "http", int? port = null)
        {
            if (!port.HasValue)
            {
                port = CreateRandomInt(9999);
            }

            return new Uri(
                string.Format("{0}://{1}:{2}",
                    scheme, CreateDomainName(), port));
        }
    }
}

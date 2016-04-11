using Lime.Messaging.Contents;
using Lime.Messaging.Resources;
using Lime.Protocol.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lime.Protocol.Serialization;

namespace Lime.Protocol.UnitTests
{
    public class Dummy
    {
        private static readonly Random _random        = new Random();
        private static readonly string _chars         = "abcdefghijklmnopqrstuvwxyz0123456789";
        private static readonly string _extendedChars = _chars + "!@#$%¨&*()_+-=\"'{}[],.;/<>:?^~\\áéíóúàèìòùºç ";

        public static int CreateRandomInt(int maxValue)
        {
            return _random.Next(maxValue);
        }

        public static string CreateRandomString(int size)
        {
            return CreateRandomString(size, _chars);
        }

        public static string CreateRandomStringExtended(int size)
        {
            return CreateRandomString(size, _extendedChars);
        }

        private static string CreateRandomString(int size, string chars)
        {
            return new string(
                Enumerable.Repeat(chars, size)
                          .Select(s => s[_random.Next(s.Length)])
                          .ToArray());
        }

        public static string CreateMessageJson()
        {
            var id = Guid.NewGuid();
            var from = Dummy.CreateNode();
            var pp = Dummy.CreateNode();
            var to = Dummy.CreateNode();

            string randomKey1 = "randomString1";
            string randomKey2 = "randomString2";
            string randomString1 = Dummy.CreateRandomStringExtended(Dummy.CreateRandomInt(100));
            string randomString2 = Dummy.CreateRandomStringExtended(Dummy.CreateRandomInt(100));

            var text = Dummy.CreateRandomString(Dummy.CreateRandomInt(50));

            return string.Format(
                "{{\"type\":\"text/plain\",\"content\":\"{0}\",\"id\":\"{1}\",\"from\":\"{2}\",\"pp\":\"{3}\",\"to\":\"{4}\",\"metadata\":{{\"{5}\":\"{6}\",\"{7}\":\"{8}\"}}}}",
                text,
                id,
                from,
                pp,
                to,
                randomKey1,
                randomString1.Escape(),
                randomKey2,
                randomString2.Escape()
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

        public static LimeUri CreateAbsoluteLimeUri()
        {
            return new LimeUri(
                string.Format("{0}://{1}/{2}",
                LimeUri.LIME_URI_SCHEME,
                CreateIdentity(),
                CreateRandomString(10)));
        }

        public static LimeUri CreateRelativeLimeUri()
        {
            return new LimeUri(
                string.Format("/{0}", CreateRandomString(10)));
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
            authentication.SetToBase64Password(CreateRandomString(8, _extendedChars));
            return authentication;
        }

        public static AuthenticationScheme[] CreateSchemeOptions()
        {
            return new AuthenticationScheme[] { AuthenticationScheme.Guest, AuthenticationScheme.Plain };
        }

        public static CancellationToken CreateCancellationToken()
        {
            return CreateCancellationToken(TimeSpan.FromSeconds(10));
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
                Id = Guid.NewGuid(),
                From = CreateNode(),
                To = CreateNode(),
                Content = content
            };
        }

        public static PlainText CreateTextContent()
        {
            return new PlainText()
            {
                Text = CreateRandomStringExtended(150)
            };
        }

        public static JsonDocument CreateJsonDocument()
        {
            return new JsonDocument(
                CreateStringObjectDictionary(),
                CreateJsonMediaType());
        }

        public static IDictionary<string, object> CreateStringObjectDictionary(bool includeDeepMembers = true)
        {
            var dictionary = new Dictionary<string, object>
            {
                {CreateRandomString(10), CreateRandomStringExtended(50)},
                {CreateRandomString(10), CreateRandomInt(50)},
                {CreateRandomString(10), DateTimeOffset.UtcNow},
            };

            if (includeDeepMembers)
            {
                dictionary.Add(CreateRandomString(10), CreateStringObjectDictionary(false));

                var list = new object[]
                {
                    CreateStringObjectDictionary(false),
                    CreateStringObjectDictionary(false),
                    CreateStringObjectDictionary(false)
                };
                dictionary.Add(CreateRandomString(10), list);
            }

            return dictionary;
        }

        public static IDictionary<string, string> CreateStringStringDictionary()
        {
            return CreateStringObjectDictionary(false).ToDictionary(d => d.Key, d => d.Value.ToString());
        }

        public static PlainDocument CreatePlainDocument()
        {
            return new PlainDocument(
                CreateRandomString(50),
                CreatePlainMediaType());
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

        public static Command CreateCommand(Document resource = null, CommandMethod method = CommandMethod.Get, CommandStatus status = CommandStatus.Pending, LimeUri uri = null)
        {
            return new Command()
            {
                From = CreateNode(),
                To = CreateNode(),
                Method = method,
                Status = status,
                Uri = uri,
                Resource = resource
            };
        }

        public static Ping CreatePing()
        {
            return new Ping();
        }

        public static DocumentCollection CreateDocumentCollection<T>(params T[] documents) 
            where T : Document, new()
        {
            var mediaType = new T().GetMediaType();

            return new DocumentCollection()
            {                
                ItemType = mediaType,
                Total = documents.Length,
                Items = documents
            };
        }


        public static Presence CreatePresence()
        {
            return new Presence()
            {
                Message = CreateRandomStringExtended(50),
                Priority = 1,
                RoutingRule = RoutingRule.IdentityByDistance,
                Status = PresenceStatus.Available,
                LastSeen = DateTimeOffset.UtcNow
            };
        }

        public static MediaType CreatePlainMediaType()
        {
            return new MediaType(
                CreateRandomString(10),
                CreateRandomString(10),
                null
                );

        }

        public static MediaType CreateJsonMediaType()
        {
            return new MediaType(
                "application",
                CreateRandomString(10),
                "json"
                );


        }

        public static Account CreateAccount()
        {
            return new Account
            {
                FullName = CreateRandomString(20),
                PhotoUri = CreateUri()
            };
        }

        public static Contact CreateContact()
        {
            return new Contact()
            {
                Identity = CreateIdentity(),
                Name = CreateRandomString(100)

            };
        }

        public static Capability CreateCapability()
        {           
            return new Capability()
            {
                ContentTypes = new[] 
                { 
                    CreateJsonMediaType(),
                    CreateJsonMediaType(),
                    CreateJsonMediaType()
                },
                ResourceTypes = new[] 
                { 
                    CreateJsonMediaType(),
                    CreateJsonMediaType(),
                    CreateJsonMediaType()
                }
            };
        }

        public static DocumentCollection CreateRoster()
        {
            return new DocumentCollection()
            {
                ItemType = MediaType.Parse(Contact.MIME_TYPE),
                Total = 3,
                Items = new[]
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

        public static ChatState CreateChatState()
        {
            return new ChatState()
            {
                State = ChatStateEvent.Composing
            };
        }

        public static Select CreateSelect()
        {
            return new Select()
            {
                Destination = CreateNode(),
                Text = CreateRandomStringExtended(100),
                Options = new[]
                {
                    CreateOption(CreateTextContent()),
                    CreateOption(CreateJsonDocument())
                },
                DefaultOption = CreateOption(CreateTextContent())
            };
        }

        public static Option CreateOption(Document value)
        {
            return new Option()
            {
                Text = CreateRandomStringExtended(10),
                Value = value
            };
        }
    }
}

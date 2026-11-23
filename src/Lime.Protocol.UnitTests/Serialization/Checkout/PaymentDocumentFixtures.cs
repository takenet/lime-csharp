using System;
using System.Collections.Generic;
using System.IO;
using Lime.Messaging;
using Lime.Messaging.Resources.Checkout;
using Lime.Protocol.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NewtonsoftEnvelopeSerializer = Lime.Protocol.Serialization.Newtonsoft.EnvelopeSerializer;
using SystemTextJsonEnvelopeSerializer = Lime.Protocol.Serialization.SystemTextJson.EnvelopeSerializer;

namespace Lime.Protocol.UnitTests.Serialization.Checkout
{
    /// <summary>
    /// Reads the payment document fixtures and wraps them in command and message envelopes.
    /// </summary>
    internal static class PaymentDocumentFixtures
    {
        public const string NewtonsoftName = "newtonsoft";
        public const string SystemTextJsonName = "system-text-json";
        public const string Command = "command";
        public const string Message = "message";

        public static readonly string[] Serializers = { NewtonsoftName, SystemTextJsonName };
        public static readonly string[] EnvelopeKinds = { Command, Message };

        public static readonly IReadOnlyDictionary<Type, string> FixturePrefixes = new Dictionary<Type, string>
        {
            { typeof(PaymentCheckoutRequest), nameof(PaymentCheckoutRequest) },
            { typeof(PaymentCheckoutResult), nameof(PaymentCheckoutResult) },
            { typeof(CustomerLastCardRequest), nameof(CustomerLastCardRequest) },
            { typeof(CustomerLastCardResult), nameof(CustomerLastCardResult) },
            { typeof(SavedCardPaymentRequest), nameof(SavedCardPaymentRequest) },
            { typeof(SavedCardPaymentResult), nameof(SavedCardPaymentResult) },
        };

        public static string Read(string fileName) =>
            File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Serialization", "Checkout", "Fixtures", fileName));

        public static IEnvelopeSerializer CreateSerializer(string name)
        {
            var resolver = new DocumentTypeResolver().WithMessagingDocuments();
            switch (name)
            {
                case NewtonsoftName:
                    return new NewtonsoftEnvelopeSerializer(resolver);
                case SystemTextJsonName:
                    return new SystemTextJsonEnvelopeSerializer(resolver);
                default:
                    throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown serializer");
            }
        }

        public static string BuildEnvelope(string kind, string mimeType, string documentJson) =>
            kind == Command
                ? "{\"id\":\"1\",\"method\":\"set\",\"uri\":\"/checkout\",\"type\":\"" + mimeType + "\",\"resource\":" + documentJson + "}"
                : "{\"id\":\"1\",\"to\":\"someone@example.com\",\"type\":\"" + mimeType + "\",\"content\":" + documentJson + "}";

        public static Document GetDocument(Envelope envelope) =>
            envelope is Command command ? command.Resource : ((Message)envelope).Content;

        public static JToken GetDocumentToken(string kind, JToken envelope) =>
            envelope[kind == Command ? "resource" : "content"];

        public static JToken Parse(string json)
        {
            using (var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None })
            {
                return JToken.ReadFrom(reader);
            }
        }

        public static (Document Read, JToken Written) RoundTrip(string serializerName, string kind, string mimeType, string documentJson)
        {
            var serializer = CreateSerializer(serializerName);
            var envelope = serializer.Deserialize(BuildEnvelope(kind, mimeType, documentJson));
            var written = Parse(serializer.Serialize(envelope));
            return (GetDocument(envelope), written);
        }
    }
}

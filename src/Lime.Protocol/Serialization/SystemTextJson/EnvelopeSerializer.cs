using Lime.Protocol.Serialization.SystemTextJson.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using STJJsonDocument = System.Text.Json.JsonDocument;

namespace Lime.Protocol.Serialization.SystemTextJson
{
    /// <summary>
    /// Serializes envelopes using the System.Text.Json library.
    /// </summary>
    /// <seealso cref="IEnvelopeSerializer" />
    public class EnvelopeSerializer : IEnvelopeSerializer
    {
        public EnvelopeSerializer(IDocumentTypeResolver documentTypeResolver)
        {
            if (documentTypeResolver == null) throw new ArgumentNullException(nameof(documentTypeResolver));

            Options = CreateOptions(documentTypeResolver);
        }

        /// <summary>
        /// Gets the <see cref="JsonSerializerOptions"/> used for serialization and deserialization.
        /// </summary>
        public JsonSerializerOptions Options { get; }

        /// <inheritdoc/>
        public string Serialize(Envelope envelope)
        {
            return JsonSerializer.Serialize(envelope, envelope.GetType(), Options);
        }

        /// <inheritdoc/>
        public Envelope Deserialize(string envelopeString)
        {
            using var document = STJJsonDocument.Parse(envelopeString);
            var root = document.RootElement;

            if (root.TryGetProperty(Message.CONTENT_KEY, out _))
            {
                return JsonSerializer.Deserialize<Message>(envelopeString, Options);
            }
            if (root.TryGetProperty(Notification.EVENT_KEY, out _))
            {
                return JsonSerializer.Deserialize<Notification>(envelopeString, Options);
            }
            if (root.TryGetProperty(Command.METHOD_KEY, out _))
            {
                return JsonSerializer.Deserialize<Command>(envelopeString, Options);
            }
            if (root.TryGetProperty(Session.STATE_KEY, out _))
            {
                return JsonSerializer.Deserialize<Session>(envelopeString, Options);
            }

            throw new ArgumentException("JSON string is not a valid envelope", nameof(envelopeString));
        }

        /// <inheritdoc/>
        public T Deserialize<T>(TextReader reader) where T : Envelope
        {
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<T>(json, Options);
        }

        /// <summary>
        /// Attempts to add a <see cref="JsonConverter"/> to the <see cref="Options"/>.
        /// Must be called before any serialization or deserialization occurs.
        /// </summary>
        /// <param name="jsonConverter">The converter to add.</param>
        /// <param name="ignoreDuplicates">Whether the provided <paramref name="jsonConverter"/> should be added when there is already one instance of that converter type.</param>
        /// <returns><see langword="true"/> if the converter was added; otherwise <see langword="false"/>.</returns>
        public bool TryAddConverter(JsonConverter jsonConverter, bool ignoreDuplicates = true)
        {
            if (jsonConverter == null) throw new ArgumentNullException(nameof(jsonConverter));

            if (!ignoreDuplicates)
            {
                foreach (var existing in Options.Converters)
                {
                    if (existing.GetType() == jsonConverter.GetType()) return false;
                }
            }

            try
            {
                // Insert before the DocumentJsonConverter (catch-all) if present
                var catchAllIndex = FindCatchAllConverterIndex(Options.Converters);
                if (catchAllIndex >= 0)
                {
                    Options.Converters.Insert(catchAllIndex, jsonConverter);
                }
                else
                {
                    Options.Converters.Add(jsonConverter);
                }
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    "TryAddConverter must be called before any serialization or deserialization occurs.",
                    ex);
            }

            return true;
        }

        internal static JsonSerializerOptions CreateOptions(IDocumentTypeResolver documentTypeResolver)
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };

            options.Converters.Add(new EnumMemberConverter());
            options.Converters.Add(new IdentityJsonConverter());
            options.Converters.Add(new NodeJsonConverter());
            options.Converters.Add(new LimeUriJsonConverter());
            options.Converters.Add(new MediaTypeJsonConverter());
            options.Converters.Add(new DateTimeOffsetJsonConverter());
            options.Converters.Add(new DateTimeJsonConverter());
            options.Converters.Add(new SessionJsonConverter());
            options.Converters.Add(new DocumentContainerJsonConverter(documentTypeResolver));
            options.Converters.Add(new DocumentCollectionJsonConverter(documentTypeResolver));
            options.Converters.Add(new DocumentJsonConverter(documentTypeResolver, options));

            return options;
        }

        private static int FindCatchAllConverterIndex(IList<JsonConverter> converters)
        {
            for (int i = 0; i < converters.Count; i++)
            {
                if (converters[i].GetType() == typeof(DocumentJsonConverter))
                    return i;
            }
            return -1;
        }
    }
}

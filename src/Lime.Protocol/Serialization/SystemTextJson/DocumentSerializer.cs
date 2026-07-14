using System;
using System.Collections.Generic;
using System.Text.Json;
using Lime.Protocol.Serialization.SystemTextJson.Converters;
using STJJsonDocument = System.Text.Json.JsonDocument;
using LimeJsonDocument = Lime.Protocol.JsonDocument;

namespace Lime.Protocol.Serialization.SystemTextJson
{
    /// <summary>
    /// System.Text.Json-based implementation of <see cref="IDocumentSerializer"/>.
    /// Reuses the <see cref="JsonSerializerOptions"/> from the <see cref="EnvelopeSerializer"/>
    /// singleton to guarantee identical converter configuration for both envelope and standalone
    /// document serialization.
    /// </summary>
    public sealed class DocumentSerializer : IDocumentSerializer
    {
        private readonly JsonSerializerOptions _options;
        private readonly IDocumentTypeResolver _resolver;

        public DocumentSerializer(EnvelopeSerializer envelopeSerializer, IDocumentTypeResolver resolver)
        {
            if (envelopeSerializer == null) throw new ArgumentNullException(nameof(envelopeSerializer));
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));

            _options = envelopeSerializer.Options;
            _resolver = resolver;
        }

        /// <inheritdoc />
        public string Serialize(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            return JsonSerializer.Serialize(document, document.GetType(), _options);
        }

        /// <inheritdoc />
        public Document Deserialize(string documentString, MediaType mediaType)
        {
            if (documentString == null) throw new ArgumentNullException(nameof(documentString));
            if (mediaType == null) throw new ArgumentNullException(nameof(mediaType));

            if (!mediaType.IsJson)
            {
                if (_resolver.TryGetTypeForMediaType(mediaType, out var documentType))
                {
                    var parseFunc = TypeUtilEx.GetParseFuncForType(documentType);
                    return (Document)parseFunc(documentString);
                }
                return new PlainDocument(documentString, mediaType);
            }

            using var stjDoc = STJJsonDocument.Parse(documentString);
            return DocumentHelper.DeserializeDocument(stjDoc.RootElement, mediaType, _options, _resolver);
        }
    }
}

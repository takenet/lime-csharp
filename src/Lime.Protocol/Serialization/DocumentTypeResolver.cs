using System;
using System.Collections.Generic;
using System.Reflection;

namespace Lime.Protocol.Serialization
{
    /// <inheritdoc />
    public class DocumentTypeResolver : IDocumentTypeResolver
    {
        private readonly Dictionary<MediaType, Type> _documentMediaTypeDictionary;

        public DocumentTypeResolver()
        {
            _documentMediaTypeDictionary = new Dictionary<MediaType, Type>();

            RegisterDocument(typeof(DocumentCollection));
            RegisterDocument(typeof(DocumentContainer));
            RegisterDocument(typeof(IdentityDocument));
            RegisterDocument(typeof(JsonDocument));
        }

        /// <inheritdoc />
        public void RegisterDocument(Type documentType)
        {
            if (documentType == null) throw new ArgumentNullException(nameof(documentType));
            if (documentType.GetTypeInfo().IsAbstract)
            {
                throw new ArgumentException("The document type should not be abstract", nameof(documentType));
            }
            if (!(Activator.CreateInstance(documentType) is IDocument document))
            {
                throw new ArgumentException("The specified type is not a valid Document");
            }

            _documentMediaTypeDictionary[document.GetMediaType()] = documentType;
        }

        /// <inheritdoc />
        public bool TryGetTypeForMediaType(MediaType mediaType, out Type documentType)
        {
            if (mediaType == null) throw new ArgumentNullException(nameof(mediaType));
            return _documentMediaTypeDictionary.TryGetValue(mediaType, out documentType);
        }
    }
}
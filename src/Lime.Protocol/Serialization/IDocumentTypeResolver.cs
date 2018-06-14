using System;
using System.Linq;
using System.Reflection;

namespace Lime.Protocol.Serialization
{
    /// <summary>
    /// Defines a service for mapping MIME media types to .NET document types.
    /// </summary>
    public interface IDocumentTypeResolver
    {
        /// <summary>
        /// RegisterDocument the specified document type.
        /// </summary>
        /// <param name="documentType"></param>
        void RegisterDocument(Type documentType);

        /// <summary>
        /// Try determine a document type for a media type.
        /// </summary>
        /// <param name="mediaType"></param>
        /// <param name="documentType"></param>
        /// <returns></returns>
        bool TryGetTypeForMediaType(MediaType mediaType, out Type documentType);
    }

    public static class DocumentTypeResolverExtensions
    {
        /// <summary>
        /// RegisterDocument the specified document type.
        /// </summary>
        /// <typeparam name="TDocument"></typeparam>
        /// <param name="typeResolver"></param>
        public static void RegisterDocument<TDocument>(this IDocumentTypeResolver typeResolver)
        {
            typeResolver.RegisterDocument(typeof(TDocument));
        }

        /// <summary>
        /// RegisterDocument all documents in the specified assembly.
        /// </summary>
        /// <param name="typeResolver"></param>
        /// <param name="assembly"></param>
        public static void RegisterAssemblyDocuments(this IDocumentTypeResolver typeResolver, Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            var documentTypes = assembly
                .DefinedTypes
                .Where(t => !t.IsAbstract && typeof(Document).GetTypeInfo().IsAssignableFrom(t));

            foreach (var type in documentTypes)
            {
                typeResolver.RegisterDocument(type.AsType());
            }
        }
    }
}
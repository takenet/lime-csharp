using System.Reflection;
using Lime.Protocol.Serialization;

namespace Lime.Messaging
{
    public static class DocumentTypeResolverExtensions
    {
        /// <summary>
        /// Registers all documents in the Lime.Messaging assembly.
        /// </summary>
        /// <param name="documentTypeResolver"></param>
        /// <returns></returns>
        public static IDocumentTypeResolver WithMessagingDocuments(this IDocumentTypeResolver documentTypeResolver)
        {
            documentTypeResolver.RegisterAssemblyDocuments(typeof(DocumentTypeResolverExtensions).GetTypeInfo().Assembly);
            return documentTypeResolver;
        }
    }
}

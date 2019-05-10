using System;
namespace Lime.Protocol
{
    /// <summary>
    /// Utilities extensions for the Document class
    /// </summary>
    public static class DocumentExtensions
    {
        /// <summary>
        /// Creates a <c>DocumentContainer</c> with <paramref name="document"/> as its <c>Value</c>
        /// </summary>
        /// <returns>The document container.</returns>
        /// <param name="document">Document to set as <c>Value</c></param>
        public static DocumentContainer ToDocumentContainer(this Document document)
        {
            return new DocumentContainer { Value = document };
        }
    }
}
namespace Lime.Protocol
{
    /// <summary>
    /// Defines a type that contains a <see cref="Document"/> instance.
    /// </summary>
    public interface IDocumentContainer
    {
        /// <summary>
        /// Gets the contained document.
        /// </summary>
        Document GetDocument();
    }
}
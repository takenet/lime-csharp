using System.Runtime.Serialization;

namespace Lime.Protocol
{
    /// <summary>
    /// Represents a node document.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public sealed class NodeDocument : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.node";
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        public NodeDocument() 
            : this(null)
            
        {
        }

        public NodeDocument(Node value)
            : base(MediaType)
        {
            Value = value;
        }

        /// <summary>
        /// The value of the document
        /// </summary>
        public Identity Value { get; set; }

        public override string ToString() => Value.ToString();

        /// <summary>
        /// Parses the string to a 
        /// IdentityDocument instance.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static NodeDocument Parse(string value) => new NodeDocument(value);
    }
}
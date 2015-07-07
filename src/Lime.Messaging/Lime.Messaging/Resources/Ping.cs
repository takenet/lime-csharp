using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Resources
{
    /// <summary>
    /// Allows the nodes to test 
    /// the network connectivity.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Ping : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.ping+json";
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="Ping"/> class.
        /// </summary>
        public Ping()
            : base(MediaType)
        {

        }
    }
}
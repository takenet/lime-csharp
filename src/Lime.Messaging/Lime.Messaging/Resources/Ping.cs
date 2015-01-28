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

        public Ping()
            : base(MediaType.Parse(MIME_TYPE))
        {

        }
    }
}
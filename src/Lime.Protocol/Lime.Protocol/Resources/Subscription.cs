using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Resources
{
    /// <summary>
    /// Represents a resource subscription information, which allows to a node receive the 
    /// updated value every time when a subscribed resource is changed in the target node. 
    /// This is useful to receive updates of changes in other identities resources, 
    /// like the presence, but is possible to subscribe to a remote resource owned by the 
    /// caller, like a resource in a server. To be able to subscribe to another node resource, 
    /// the subscriber must have a get delegation for the resource in the publisher node.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public partial class Subscription : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.subscription+json";

        public Subscription()
            : base(MediaType.Parse(MIME_TYPE))
        {

        }

        /// <summary>
        /// The identity of the owner of the resource. 
        /// The default value is the identity of the 
        /// from property of the envelope.
        /// </summary>
        [DataMember(Name = "owner")]
        public Identity Owner { get; set; }

        /// <summary>
        /// The MIME type of the resource for subscription.
        /// </summary>
        [DataMember(Name = "typr")]
        public MediaType Type { get; set; }
    }
}

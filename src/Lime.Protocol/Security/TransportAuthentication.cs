using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Security
{
    /// <summary>
    /// Defines a transport layer 
    /// authentication scheme. 
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class TransportAuthentication : Authentication
    {
        public TransportAuthentication()
            : base(AuthenticationScheme.Transport)
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Security
{
    /// <summary>
    /// Base class for the supported 
    /// authentication schemes
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public abstract class Authentication
    {
        private AuthenticationScheme _scheme;

        public Authentication(AuthenticationScheme scheme)
        {
            _scheme = scheme;
        }

        public AuthenticationScheme GetAuthenticationScheme()
        {
            return _scheme;
        }
    }
}

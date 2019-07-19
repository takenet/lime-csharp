using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    /// <summary>
    /// Represents an identity in a domain.
    /// </summary>
    public interface IIdentity
    {
        /// <summary>
        /// Identity unique name on his domain.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Network domain name of the identity.
        /// </summary>
        string Domain { get; }
    }
}

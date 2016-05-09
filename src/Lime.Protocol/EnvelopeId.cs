using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    /// <summary>
    /// Utility class for generating envelope ids.
    /// </summary>
    public static class EnvelopeId
    {
        /// <summary>
        /// Generates a new envelope identifier.
        /// </summary>
        /// <returns></returns>
        public static string NewId() => Guid.NewGuid().ToString();
    }
}

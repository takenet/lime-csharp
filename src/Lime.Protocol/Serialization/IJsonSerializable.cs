using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Serialization
{
    /// <summary>
    /// Define types that can be
    /// serialized to JSON
    /// </summary>
    public interface IJsonSerializable
    {
        /// <summary>
        /// Serializes the instance value
        /// to a JSON string representation
        /// </summary>
        /// <returns></returns>
        string ToJson();
    }
}

using Newtonsoft.Json;
using System;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    class NodeJsonConverter : StringBasedTypeJsonConverter<Node>
    {
        protected override Node CreateInstance(string tokenValue)
        {
            return Node.Parse(tokenValue);
        }
    }
}
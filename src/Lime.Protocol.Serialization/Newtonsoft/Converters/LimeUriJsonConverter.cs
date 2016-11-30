using Newtonsoft.Json;
using System;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    public class LimeUriJsonConverter : StringBasedTypeJsonConverter<LimeUri>
    {
        protected override LimeUri CreateInstance(string tokenValue)
        {
            return LimeUri.Parse(tokenValue);
        }
    }
}
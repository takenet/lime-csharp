using Newtonsoft.Json;
using System;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    public class MediaTypeJsonConverter : StringBasedTypeJsonConverter<MediaType>
    {
        protected override MediaType CreateInstance(string tokenValue)
        {
            return MediaType.Parse(tokenValue);
        }
    }
}
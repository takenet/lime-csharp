using System;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    class UriJsonConverter : StringBasedTypeJsonConverter<Uri>
    {
        protected override Uri CreateInstance(string tokenValue)
        {
            return new Uri(tokenValue);
        }
    }
}

using System;
using Newtonsoft.Json;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    public class UriJsonConverter : StringBasedTypeJsonConverter<Uri>
    {
        protected override Uri CreateInstance(string tokenValue)
        {
            return new Uri(tokenValue);
        }

        public override void WriteJson(JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            var uri = value as Uri;
            if (uri != null)
            {
                writer.WriteValue(Uri.EscapeUriString(uri.ToString()));
            }
            else
            {
                writer.WriteNull();
            }
        }
    }
}

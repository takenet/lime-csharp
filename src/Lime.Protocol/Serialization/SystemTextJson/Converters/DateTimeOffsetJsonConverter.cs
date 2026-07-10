using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lime.Protocol.Serialization.SystemTextJson.Converters
{
    /// <summary>
    /// Converts <see cref="DateTimeOffset"/> to and from JSON, using the format
    /// <c>yyyy-MM-ddTHH:mm:ss.fffZ</c> to match the Newtonsoft.Json default behavior.
    /// </summary>
    public class DateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
    {
        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return DateTimeOffset.Parse(
                value,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.UtcDateTime.ToString(DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lime.Protocol.Serialization.SystemTextJson.Converters
{
    /// <summary>
    /// A <see cref="JsonConverter{T}"/> for <see cref="DateTime"/> values, using the
    /// format <c>yyyy-MM-ddTHH:mm:ss.fffZ</c> (UTC, matching Newtonsoft's default behaviour).
    /// </summary>
    internal sealed class DateTimeJsonConverter : JsonConverter<DateTime>
    {
        private const string Format = "yyyy-MM-ddTHH:mm:ss.fffZ";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (DateTime.TryParseExact(str, Format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result))
                return result;

            return DateTime.Parse(str, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture));
        }
    }
}

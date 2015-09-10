using Newtonsoft.Json;

namespace Lime.Protocol.Serialization.Newtonsoft.Extensions
{
    public static class JsonWriterExtensions
    {
        public static void WriteValueIfNotDefault<T>(this JsonWriter writer, string propertyName, T value)
        {
            if (value != null && !value.Equals(default (T)))
            {
                writer.WritePropertyName(propertyName);
                writer.WriteValue(value);
            }
        }

        public static void WriteValueIfNotDefaultAsString<T>(this JsonWriter writer, string propertyName, T value)
        {
            if (value != null && !value.Equals(default (T)))
            {
                writer.WritePropertyName(propertyName);
                writer.WriteValue(value.ToString());
            }
        }
    }
}
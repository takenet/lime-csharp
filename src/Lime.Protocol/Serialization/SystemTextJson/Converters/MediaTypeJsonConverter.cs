namespace Lime.Protocol.Serialization.SystemTextJson.Converters
{
    /// <summary>
    /// Converts <see cref="MediaType"/> to and from JSON strings.
    /// </summary>
    public class MediaTypeJsonConverter : StringBasedJsonConverter<MediaType>
    {
        protected override MediaType CreateInstance(string value) => MediaType.Parse(value);
    }
}

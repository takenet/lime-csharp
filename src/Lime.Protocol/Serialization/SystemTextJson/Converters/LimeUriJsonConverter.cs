namespace Lime.Protocol.Serialization.SystemTextJson.Converters
{
    /// <summary>
    /// Converts <see cref="LimeUri"/> to and from JSON strings.
    /// </summary>
    public class LimeUriJsonConverter : StringBasedJsonConverter<LimeUri>
    {
        protected override LimeUri CreateInstance(string value) => LimeUri.Parse(value);
    }
}

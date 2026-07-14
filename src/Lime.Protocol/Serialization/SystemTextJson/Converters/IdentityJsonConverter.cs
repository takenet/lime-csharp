namespace Lime.Protocol.Serialization.SystemTextJson.Converters
{
    /// <summary>
    /// Converts <see cref="Identity"/> to and from JSON strings.
    /// </summary>
    public class IdentityJsonConverter : StringBasedJsonConverter<Identity>
    {
        protected override Identity CreateInstance(string value) => Identity.Parse(value);
    }
}

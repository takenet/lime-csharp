namespace Lime.Protocol.Serialization.SystemTextJson.Converters
{
    /// <summary>
    /// Converts <see cref="Node"/> to and from JSON strings.
    /// </summary>
    public class NodeJsonConverter : StringBasedJsonConverter<Node>
    {
        protected override Node CreateInstance(string value) => Node.Parse(value);
    }
}

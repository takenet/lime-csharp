namespace Lime.Protocol.Serialization.Json.Converters
{
    public class NodeJsonConverter : StringJsonConverterBase<Node>
    {
        protected override Node Parse(string value) => value;
    }
}
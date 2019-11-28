namespace Lime.Protocol.Serialization.Json.Converters
{
    public class LimeUriJsonConverter : StringJsonConverterBase<Node>
    {
        protected override Node Parse(string value) => value;
    }
}
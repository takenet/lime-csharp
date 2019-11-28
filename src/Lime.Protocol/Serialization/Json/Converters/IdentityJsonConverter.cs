namespace Lime.Protocol.Serialization.Json.Converters
{
    public class IdentityJsonConverter : StringJsonConverterBase<Node>
    {
        protected override Node Parse(string value) => value;
    }
}
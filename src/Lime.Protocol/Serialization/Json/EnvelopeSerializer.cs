using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lime.Protocol.Serialization.Json.Converters;

namespace Lime.Protocol.Serialization.Json
{
    public class EnvelopeSerializer : IEnvelopeSerializer
    {
        private readonly JsonSerializerOptions _options;

        public EnvelopeSerializer()
        {
            var converters = new List<JsonConverter>();
            converters.AddRange(new JsonConverter[]
            {
                new IdentityJsonConverter(),
                new LimeUriJsonConverter(),
                new MediaTypeJsonConverter(),
                new NodeJsonConverter()
            });
            
            _options = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true,
                WriteIndented = false,
            };
            foreach (var converter in converters)
            {
                _options.Converters.Add(converter);
            }
        }
        
        public string Serialize(Envelope envelope)
        {
            return JsonSerializer.Serialize(envelope, _options);
            
        }

        public Envelope Deserialize(string envelopeString)
        {
            
            
            throw new System.NotImplementedException();
        }
    }
}
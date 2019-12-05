using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lime.Protocol.Serialization.Json.Converters;

namespace Lime.Protocol.Serialization.Json
{
    public class EnvelopeSerializer : IEnvelopeSerializer
    {
        private readonly JsonSerializerOptions _options;


        private readonly byte[] _statePropertyName = Encoding.UTF8.GetBytes(Session.STATE_KEY);

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
            // TODO: get direct from stream
            var envelopeUtf8 = new Memory<byte>(Encoding.UTF8.GetBytes(envelopeString));
            //var reader = new Utf8JsonReader(envelopeUtf8.Span);
            var jsonDocument = System.Text.Json.JsonDocument.Parse(envelopeUtf8);

            if (jsonDocument.RootElement.TryGetProperty(_statePropertyName.AsSpan(), out _))
            {
                
            }

            //JsonSerializer.Deserialize(jsonDocument.RootElement, typeof(Session));
            
            
            throw new System.NotImplementedException();
        }
    }
    
    // https://github.com/dotnet/corefx/issues/37564
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    internal readonly ref struct JsonElementSerializer
    {
        static readonly FieldInfo JsonDocumentField = typeof(JsonElement).GetField("_parent", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo JsonDocumentUtf8JsonField = typeof(JsonDocument).GetField("_utf8Json", BindingFlags.NonPublic | BindingFlags.Instance);

        ReadOnlyMemory<byte> Value { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JsonElementSerializer(JsonElement jsonElement)
        {
            if(JsonDocumentField == null) throw new ArgumentNullException(nameof(JsonDocumentField));
            if(JsonDocumentUtf8JsonField == null) throw new ArgumentNullException(nameof(JsonDocumentUtf8JsonField));
            var jsonDocument = JsonDocumentField.GetValue(jsonElement);
            Value = (ReadOnlyMemory<byte>) JsonDocumentUtf8JsonField.GetValue(jsonDocument);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ToObject<T>(JsonSerializerOptions jsonSerializerOptions = null)
        {
            return JsonSerializer.Deserialize<T>(Value.Span, jsonSerializerOptions);
        }

    }
}
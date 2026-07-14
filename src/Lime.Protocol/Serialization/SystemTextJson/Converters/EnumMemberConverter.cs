using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lime.Protocol.Serialization.SystemTextJson.Converters
{
    /// <summary>
    /// A <see cref="JsonConverterFactory"/> that creates converters for enum types,
    /// using <see cref="EnumMemberAttribute"/> values as the JSON string representations.
    /// Also handles nullable enum types.
    /// </summary>
    public class EnumMemberConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (typeToConvert.IsEnum) return true;
            var underlying = Nullable.GetUnderlyingType(typeToConvert);
            return underlying != null && underlying.IsEnum;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var enumType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
            var isNullable = Nullable.GetUnderlyingType(typeToConvert) != null;

            var converterType = isNullable
                ? typeof(NullableEnumMemberJsonConverter<>).MakeGenericType(enumType)
                : typeof(EnumMemberJsonConverter<>).MakeGenericType(enumType);

            return (JsonConverter)Activator.CreateInstance(converterType);
        }
    }

    internal sealed class EnumMemberJsonConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        private readonly Dictionary<string, T> _stringToEnum;
        private readonly Dictionary<T, string> _enumToString;

        public EnumMemberJsonConverter()
        {
            _stringToEnum = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            _enumToString = new Dictionary<T, string>();

            foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var enumValue = (T)field.GetValue(null);
                var enumMember = field.GetCustomAttribute<EnumMemberAttribute>();
                var jsonValue = enumMember?.Value ?? field.Name.ToCamelCase();

                _stringToEnum[jsonValue] = enumValue;
                if (!_enumToString.ContainsKey(enumValue))
                {
                    _enumToString[enumValue] = jsonValue;
                }
            }
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (str != null && _stringToEnum.TryGetValue(str, out var value))
                return value;
            throw new JsonException($"Unknown {typeof(T).Name} value: '{str}'");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (_enumToString.TryGetValue(value, out var str))
                writer.WriteStringValue(str);
            else
                writer.WriteStringValue(value.ToString().ToCamelCase());
        }
    }

    internal sealed class NullableEnumMemberJsonConverter<T> : JsonConverter<T?> where T : struct, Enum
    {
        private readonly EnumMemberJsonConverter<T> _innerConverter = new EnumMemberJsonConverter<T>();

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            return _innerConverter.Read(ref reader, typeof(T), options);
        }

        public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }
            _innerConverter.Write(writer, value.Value, options);
        }
    }
}

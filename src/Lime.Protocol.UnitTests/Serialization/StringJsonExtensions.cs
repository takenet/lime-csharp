using System;
using System.Collections.Generic;
using System.Text;
using Lime.Protocol.Serialization;
using System.Globalization;
using System.Linq;

namespace Lime.Protocol.UnitTests.Serialization
{
    public static class StringJsonExtensions
    {
        public static bool HasValidJsonStackedBrackets(this string json)
        {
            if (json.Length == 0) return false;
            if (json[0] != '{' || json[json.Length - 1] != '}') return false;
            var openedBrackets = 0;
            var jsonStarted = false;
            var insideQuotes = false;
            var isEscaping = false;

            foreach (var c in json)
            {
                if (c == '"' && !isEscaping)
                {
                    insideQuotes = !insideQuotes;
                }

                if (!insideQuotes)
                {
                    if (c == '{')
                    {
                        if (!jsonStarted)
                        {
                            jsonStarted = true;
                        }
                        openedBrackets++;
                    }
                    else if (c == '}')
                    {
                        openedBrackets--;
                    }
                }
                else
                {
                    if (isEscaping)
                    {
                        isEscaping = false;
                    }
                    else if (c == '\\')
                    {
                        isEscaping = true;
                    }
                }
            }

            return jsonStarted && openedBrackets == 0;
        }

        public static bool ContainsJsonProperty(this string json, string key, object value)
        {
            if (value is int || value is long || value is bool)
            {
                return json.Contains(
                    $"\"{key}\":{value.ToString().ToLower()}");
            }

            if (value is double)
            {
                return json.Contains(
                    $"\"{key}\":{((double) value).ToString(CultureInfo.InvariantCulture)}");
            }

            if (value is DateTime)
            {
                return json.Contains(
                    $"\"{key}\":\"{((DateTime) value).ToUniversalTime().ToString(TextJsonWriter.DATE_FORMAT, CultureInfo.InvariantCulture)}\"");
            }

            if (value is DateTimeOffset)
            {
                return json.Contains(
                    $"\"{key}\":\"{((DateTimeOffset) value).ToUniversalTime().ToString(TextJsonWriter.DATE_FORMAT, CultureInfo.InvariantCulture)}\"");
            }

            if (value.GetType().IsEnum)
            {
                return json.Contains(
                    $"\"{key}\":\"{value.ToString().ToCamelCase()}\"");
            }

            if (value is IDictionary<string, object>)
            {
                var dictionary = (IDictionary<string, object>)value;
                if (!json.ContainsJsonKey(key))
                {
                    return false;
                }

                if (dictionary.Where(p => !p.Value.GetType().IsArray).Any(kv => !json.ContainsJsonProperty(kv.Key, kv.Value)))
                {
                    return false;
                }
                return true;
            }

            if (value.GetType().IsArray)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendFormat("\"{0}\":[", key);

                foreach (var v in (Array)value)
                {                    
                    if (v is int || v is long || v is bool)
                    {
                        stringBuilder.AppendFormat("{0},", v);
                    }
                    else if (v.GetType().IsEnum)
                    {
                        stringBuilder.AppendFormat("\"{0}\",", v.ToString().ToCamelCase());
                    }
                    else if (v.GetType().IsArray)
                    {
                        throw new NotSupportedException("Cannot check for array properties");
                    }
                    else
                    {
                        stringBuilder.AppendFormat("\"{0}\",", v.ToString().Escape());
                    }
                }

                stringBuilder.Append(']');

                return json.Contains(stringBuilder.ToString().Replace(",]", "]"));
            }

            return json.Contains(
                $"\"{key}\":\"{value.ToString().Escape()}\"");
        }

        public static bool ContainsJsonKey(this string json, string key)
        {
            return json.Contains(
                $"\"{key.Escape()}\":");
        }
    }
}

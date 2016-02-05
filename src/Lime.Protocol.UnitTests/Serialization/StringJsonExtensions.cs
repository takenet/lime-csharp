using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lime.Protocol.Serialization;
using System.Collections;
using System.Globalization;

namespace Lime.Protocol.UnitTests.Serialization
{
    public static class StringJsonExtensions
    {
        public static bool HasValidJsonStackedBrackets(this string json)
        {
            var openedBrackets = 0;
            var jsonStarted = false;
            var insideQuotes = false;
            var previousC = default(char);

            foreach (var c in json)
            {
                if (c == '"' && !previousC.Equals('\\'))
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
                previousC = c;
            }

            return jsonStarted && openedBrackets == 0;
        }

        public static bool ContainsJsonProperty(this string json, string key, object value)
        {
            if (value is int || value is long || value is bool)
            {
                return json.Contains(
                    string.Format("\"{0}\":{1}",
                        key,
                        value.ToString().ToLower()));
            }

            if (value is double)
            {
                return json.Contains(
                    string.Format("\"{0}\":{1}",
                        key,
                        ((double)value).ToString(CultureInfo.InvariantCulture)));
            }

            if (value is DateTime)
            {
                return json.Contains(
                    string.Format("\"{0}\":\"{1}\"",
                        key,
                        ((DateTime)value).ToUniversalTime().ToString(TextJsonWriter.DATE_FORMAT, CultureInfo.InvariantCulture)));
            }

            if (value is DateTimeOffset)
            {
                return json.Contains(
                    string.Format("\"{0}\":\"{1}\"",
                        key,
                        ((DateTimeOffset)value).ToUniversalTime().ToString(TextJsonWriter.DATE_FORMAT, CultureInfo.InvariantCulture)));
            }

            if (value.GetType().IsEnum)
            {
                return json.Contains(
                    string.Format("\"{0}\":\"{1}\"",
                        key,
                        value.ToString().ToCamelCase()));
            }

            if (value is IDictionary<string, object>)
            {
                throw new NotSupportedException("Cannot check for dictionary properties");
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

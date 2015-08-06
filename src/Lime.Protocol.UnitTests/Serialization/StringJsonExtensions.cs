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
            int openedBrackets = 0;
            bool jsonStarted = false;

            foreach (var c in json)
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
            else if (value.GetType().IsEnum)
            {
                return json.Contains(
                    string.Format("\"{0}\":\"{1}\"",
                        key,
                        value.ToString().ToCamelCase()));
            }
            else if (value is IDictionary<string, object>)
            {
                throw new NotSupportedException("Cannot check for dictionary properties");
            }
            else if (value.GetType().IsArray)
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
                        stringBuilder.AppendFormat("\"{0}\",", v);
                    }
                }

                stringBuilder.Append(']');

                return json.Contains(stringBuilder.ToString().Replace(",]", "]"));
            }
            else
            {
                return json.Contains(
                    string.Format("\"{0}\":\"{1}\"",
                        key,
                        value));
            }
        }

        public static bool ContainsJsonKey(this string json, string key)
        {
            return json.Contains(
                string.Format("\"{0}\":", key));
        }
    }
}

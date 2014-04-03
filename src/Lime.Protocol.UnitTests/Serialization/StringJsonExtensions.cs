using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lime.Protocol.Serialization;

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
                        value));
            }
            else if (value.GetType().IsEnum)
            {
                return json.Contains(
                    string.Format("\"{0}\":\"{1}\"",
                        key,
                        value.ToString().ToCamelCase()));
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

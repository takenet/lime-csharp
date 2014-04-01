using Lime.Protocol.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lime.Protocol.Serialization
{
    public class InternalJsonSerializer : IEnvelopeSerializer
    {
        #region IEnvelopeSerializer Members

        public string Serialize(Envelope envelope)
        {
            return envelope.ToJson();
        }

        public Envelope Deserialize(string envelopeString)
        {
            var jsonDictionary = ParseJson(envelopeString); 

            if (jsonDictionary.ContainsKey("content"))
            {
                return Message.FromJsonObject(jsonDictionary);
            }
            else if (jsonDictionary.ContainsKey("event"))
            {
                return Notification.FromJsonObject(jsonDictionary);
            }
            else if (jsonDictionary.ContainsKey("method"))
            {
                return Command.FromJsonObject(jsonDictionary);
            }
            else if (jsonDictionary.ContainsKey("state"))
            {
                return Session.FromJsonObject(jsonDictionary);
            }
            else
            {
                throw new ArgumentException("JSON string is not a valid envelope");
            }
        }

        #endregion


        #region JSON Parser

        private static Regex _unicodeRegex = new Regex(@"\\u([0-9a-z]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// <see cref="http://stackoverflow.com/questions/1207731/how-can-i-deserialize-json-to-a-simple-dictionarystring-string-in-asp-net"/>
        /// </summary>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        private static JsonObject ParseJson(string jsonString)
        {
            try
            {
                int end;
                return ParseJson(jsonString, 0, out end);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Invalid or not supported JSON string", "jsonString", ex);
            }
        }
        
        private static JsonObject ParseJson(string jsonString, int start, out int end)
        {
            var resultDictionary = new JsonObject();

            bool escBegin = false;
            bool escEnd = false;
            bool inQuotes = false;
            string key = null;
            int cEnd;
            var tokenBuilder = new StringBuilder();
            JsonObject child = null;
            IList<object> arrayList = null;
            int autoKey = 0;

            for (int i = start; i < jsonString.Length; i++)
            {
                char c = jsonString[i];

                if (c == '\\')
                    escBegin = !escBegin;

                if (!escBegin)
                {
                    if (c == '"')
                    {
                        inQuotes = !inQuotes;
                        if (!inQuotes && arrayList != null)
                        {
                            tokenBuilder.Append(c);
                            arrayList.Add(ParseValue(tokenBuilder.ToString()));
                            tokenBuilder.Length = 0;
                            continue;
                        }
                    }
                    if (!inQuotes)
                    {
                        switch (c)
                        {
                            case '{':
                                if (i != start)
                                {
                                    child = ParseJson(jsonString, i, out cEnd);
                                    if (arrayList != null)
                                    {
                                        arrayList.Add(child);
                                    }
                                    else
                                    {
                                        resultDictionary.Add(key, child);
                                        key = null;
                                    }
                                    i = cEnd;
                                }
                                continue;
                            case '}':
                                end = i;
                                if (key != null)
                                {
                                    if (arrayList != null)
                                        resultDictionary.Add(key, arrayList);
                                    else
                                        resultDictionary.Add(key, ParseValue(tokenBuilder.ToString()));
                                }
                                return resultDictionary;
                            case '[':
                                arrayList = new List<object>();
                                continue;
                            case ']':
                                if (key == null)
                                {
                                    key = "array" + autoKey.ToString();
                                    autoKey++;
                                }
                                if (arrayList != null && tokenBuilder.Length > 0)
                                {
                                    arrayList.Add(ParseValue(tokenBuilder.ToString()));
                                    tokenBuilder.Length = 0;
                                }
                                resultDictionary.Add(key, arrayList);
                                arrayList = null;
                                key = null;
                                continue;
                            case ',':
                                if (arrayList == null && key != null)
                                {
                                    resultDictionary.Add(key, ParseValue(tokenBuilder.ToString()));
                                    key = null;
                                    tokenBuilder.Length = 0;
                                }
                                if (arrayList != null && tokenBuilder.Length > 0)
                                {
                                    arrayList.Add(ParseValue(tokenBuilder.ToString()));
                                    tokenBuilder.Length = 0;
                                }
                                continue;
                            case ':':
                                key = ParseValue(tokenBuilder.ToString()).ToString();
                                tokenBuilder.Length = 0;
                                continue;
                            case ' ':
                                continue;
                        }
                    }
                }

                tokenBuilder.Append(c);

                if (escEnd)
                    escBegin = false;

                if (escBegin)
                    escEnd = true;
                else
                    escEnd = false;
            }
            end = jsonString.Length - 1;
            return resultDictionary; //theoretically shouldn't ever get here
        }

        private static object ParseValue(string value)
        {
            if (value[0] == '"' &&
                value[value.Length - 1] == '"')
            {
                value = value.Trim('"');

                return Regex.Unescape(
                    _unicodeRegex.Replace(value, match => char.ConvertFromUtf32(
                        Int32.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber))));
            }
            else
            {
                bool boolResult;
                int intResult;

                if (bool.TryParse(value, out boolResult))
                {
                    return boolResult;
                }
                else if (int.TryParse(value, out intResult))
                {
                    return intResult;
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion

    }
}

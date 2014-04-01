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
            throw new NotImplementedException();
        }

        public Envelope Deserialize(string envelopeString)
        {
            var jsonDictionary = ParseJson(envelopeString); 

            if (jsonDictionary.ContainsKey("content"))
            {
                return this.DeserializeAsMessage(jsonDictionary);
            }
            else if (jsonDictionary.ContainsKey("event"))
            {
                return this.DeserializeAsNotification(envelopeString);
            }
            else if (jsonDictionary.ContainsKey("method"))
            {
                return DeserializeAsCommand(jsonDictionary);
            }
            else if (jsonDictionary.ContainsKey("state"))
            {
                return DeserializeAsSession(jsonDictionary);
            }
            else
            {
                throw new ArgumentException("JSON string is not a valid envelope");
            }
        }



        #endregion

        private Session DeserializeAsSession(Dictionary<string, object> jsonDictionary)
        {
            var session = new Session();
            session.From = Node.ParseNode((string)jsonDictionary["from"]);

            throw new NotImplementedException();
        }

        private Command DeserializeAsCommand(Dictionary<string, object> jsonDictionary)
        {
            throw new NotImplementedException();
        }

        private Notification DeserializeAsNotification(string envelopeString)
        {
            throw new NotImplementedException();
        }

        private Message DeserializeAsMessage(Dictionary<string, object> jsonDictionary)
        {
            throw new NotImplementedException();
        }



        /// <summary>
        /// <see cref="http://stackoverflow.com/questions/1207731/how-can-i-deserialize-json-to-a-simple-dictionarystring-string-in-asp-net"/>
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        private static Dictionary<string, object> ParseJson(string json)
        {
            int end;
            return ParseJson(json, 0, out end);
        }

        private static Regex _regex = new Regex(@"\\u([0-9a-z]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static Dictionary<string, object> ParseJson(string json, int start, out int end)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            bool escbegin = false;
            bool escend = false;
            bool inquotes = false;
            string key = null;
            int cend;
            StringBuilder sb = new StringBuilder();
            Dictionary<string, object> child = null;
            List<object> arraylist = null;
            int autoKey = 0;
            for (int i = start; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '\\') escbegin = !escbegin;
                if (!escbegin)
                {
                    if (c == '"')
                    {
                        inquotes = !inquotes;
                        if (!inquotes && arraylist != null)
                        {
                            arraylist.Add(DecodeString(_regex, sb.ToString()));
                            sb.Length = 0;
                        }
                        continue;
                    }
                    if (!inquotes)
                    {
                        switch (c)
                        {
                            case '{':
                                if (i != start)
                                {
                                    child = ParseJson(json, i, out cend);
                                    if (arraylist != null) arraylist.Add(child);
                                    else
                                    {
                                        dict.Add(key, child);
                                        key = null;
                                    }
                                    i = cend;
                                }
                                continue;
                            case '}':
                                end = i;
                                if (key != null)
                                {
                                    if (arraylist != null) dict.Add(key, arraylist);
                                    else dict.Add(key, DecodeString(_regex, sb.ToString()));
                                }
                                return dict;
                            case '[':
                                arraylist = new List<object>();
                                continue;
                            case ']':
                                if (key == null)
                                {
                                    key = "array" + autoKey.ToString();
                                    autoKey++;
                                }
                                if (arraylist != null && sb.Length > 0)
                                {
                                    arraylist.Add(sb.ToString());
                                    sb.Length = 0;
                                }
                                dict.Add(key, arraylist);
                                arraylist = null;
                                key = null;
                                continue;
                            case ',':
                                if (arraylist == null && key != null)
                                {
                                    dict.Add(key, DecodeString(_regex, sb.ToString()));
                                    key = null;
                                    sb.Length = 0;
                                }
                                if (arraylist != null && sb.Length > 0)
                                {
                                    arraylist.Add(sb.ToString());
                                    sb.Length = 0;
                                }
                                continue;
                            case ':':
                                key = DecodeString(_regex, sb.ToString());
                                sb.Length = 0;
                                continue;
                        }
                    }
                }
                sb.Append(c);
                if (escend) escbegin = false;
                if (escbegin) escend = true;
                else escend = false;
            }
            end = json.Length - 1;
            return dict; //theoretically shouldn't ever get here
        }
        private static string DecodeString(Regex regex, string str)
        {
            return Regex.Unescape(
                regex.Replace(str, match => char.ConvertFromUtf32(
                    Int32.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber))));
        }
    }
}

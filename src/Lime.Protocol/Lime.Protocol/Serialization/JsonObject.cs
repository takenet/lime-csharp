using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lime.Protocol.Serialization
{
    /// <summary>
    /// Represents a JSON object instance
    /// </summary>
    public sealed class JsonObject : Dictionary<string, object>
    {
        private static Regex _unicodeRegex = new Regex(@"\\u([0-9a-z]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonObject"/> class.
        /// </summary>
        public JsonObject()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonObject"/> class.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <exception cref="System.ArgumentNullException">dictionary</exception>
        public JsonObject(IDictionary<string, object> dictionary)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));

            foreach (var key in dictionary.Keys)
            {
                Add(key, dictionary[key]);
            }
        }

        #endregion

        #region Public Methods

        public Array GetArrayOrNull(Type itemType, string key, Func<object, object> castFunc)
        {
            Array array = null;

            if (base.ContainsKey(key))
            {
                var list = base[key] as IList;

                if (list == null)
                {
                    throw new ArgumentException(string.Format("The dictionary value for key '{0}' is not IList", key));
                }

                array = Array.CreateInstance(itemType, list.Count);

                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];

                    array.SetValue(castFunc(item), i);
                }
            }

            return array;
        }

        public Array GetEnumArrayOrNull(Type enumType, string key)
        {
            Array array = null;

            if (base.ContainsKey(key))
            {
                var value = base[key];
                if (value != null)
                {
                    var list = base[key] as IList;

                    if (list == null)
                    {
                        throw new ArgumentException(string.Format("The dictionary value for key '{0}' is not IList", key));
                    }

                    array = Array.CreateInstance(enumType, list.Count);

                    for (int i = 0; i < list.Count; i++)
                    {
                        var item = (string)list[i];
                        var itemValue = TypeUtil.ParseEnum(enumType, item);
                        array.SetValue(itemValue, i);
                    }
                }
            }

            return array;
        }

        public object GetEnumValueOrNull(Type enumType, string key)
        {
            object enumValue = null;

            if (base.ContainsKey(key))
            {
                var value = base[key];
                if (value != null)
                {
                    enumValue = TypeUtil.ParseEnum(enumType, (string)value);
                }
            }

            return enumValue;
        }

        public T GetValueOrDefault<T>(string key, Func<object, T> castFunc = null)
        {
            T value = default(T);

            if (base.ContainsKey(key))
            {
                if (castFunc == null)
                {
                    castFunc = (i) => (T)i;
                }

                value = castFunc(base[key]);
            }

            return value;
        }

        public Nullable<T> GetValueOrNull<T>(string key, Func<object, T> castFunc = null) where T : struct
        {
            Nullable<T> value = null;

            if (base.ContainsKey(key) &&
                base[key] != null)
            {
                if (castFunc == null)
                {
                    castFunc = (v) => (T)v;
                }

                value = castFunc(base[key]);
            }

            return value;
        }

        public object GetValueOrNull(string key, Func<string, object> castFunc)
        {
            object value = null;

            if (base.ContainsKey(key) &&
                base[key] is string)
            {
                value = castFunc((string)base[key]);
            }

            return value;
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// <a href="http://stackoverflow.com/questions/1207731/how-can-i-deserialize-json-to-a-simple-dictionarystring-string-in-asp-net"/>
        /// </summary>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        public static JsonObject ParseJson(string jsonString)
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
                            case '\t':
                            case '\r':
                            case '\n':
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
                long longResult;
                double doubleResult;

                if (bool.TryParse(value, out boolResult))
                {
                    return boolResult;
                }
                else if (long.TryParse(value, out longResult))
                {
                    return longResult;
                }
                else if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out doubleResult))
                {
                    return doubleResult;
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
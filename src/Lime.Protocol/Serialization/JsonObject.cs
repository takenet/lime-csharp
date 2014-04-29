using System;
using System.Collections;
using System.Collections.Generic;
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

        public JsonObject()
        {

        }

        public JsonObject(IDictionary<string, object> dictionary)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException("dictionary");
            }

            foreach (var key in dictionary.Keys)
            {
                Add(key, dictionary[key]);
            }
        }

        #endregion

        #region Public Methods

        public TEnum[] GetEnumArrayOrNull<TEnum>(string key) where TEnum : struct
        {
            TEnum[] array = null;

            if (base.ContainsKey(key))
            {
                var list = base[key] as IList;

                if (list == null)
                {
                    throw new ArgumentException(string.Format("The dictionary value for key '{0}' is not IList", key));
                }

                array = new TEnum[list.Count];

                var valueType = typeof(TEnum);

                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    array[i] = TypeUtil.GetEnumValue<TEnum>(item.ToString());
                }
            }

            return array;
        }

        public T[] GetArrayOrNull<T>(string key, Func<object, T> castFunc = null)
        {
            T[] array = null;

            if (base.ContainsKey(key))
            {
                var list = base[key] as IList;

                if (list == null)
                {
                    throw new ArgumentException(string.Format("The dictionary value for key '{0}' is not IList", key));
                }

                array = new T[list.Count];

                var valueType = typeof(T);

                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];

                    if (castFunc == null)
                    {
                        if (item is T)
                        {
                            array[i] = (T)item;
                        }
                        else
                        {
                            throw new ArgumentException("Item type not supported for conversion");
                        }
                    }
                    else
                    {
                        array[i] = castFunc(item);
                    }
                }
            }

            return array;
        }

        public Nullable<TEnum> GetEnumValueOrNull<TEnum>(string key) where TEnum : struct
        {
            Nullable<TEnum> value = null;

            if (base.ContainsKey(key))
            {
                value = TypeUtil.GetEnumValue<TEnum>(base[key].ToString());
            }

            return value;
        }

        public TEnum GetEnumValueOrDefault<TEnum>(string key) where TEnum : struct
        {
            TEnum value = default(TEnum);

            if (base.ContainsKey(key))
            {
                value = TypeUtil.GetEnumValue<TEnum>(base[key].ToString());
            }

            return value;
        }

        public T GetValueOrDefault<T>(string key)
        {
            T value = default(T);

            if (base.ContainsKey(key))
            {
                value = (T)base[key];
            }

            return value;
        }

        public T GetValueOrDefault<T>(string key, Func<object, T> castFunc)
        {
            T value = default(T);

            if (base.ContainsKey(key))
            {
                value = castFunc(base[key]);
            }

            return value;
        }

        public Nullable<T> GetValueOrNull<T>(string key) where T : struct
        {
            Nullable<T> value = null;

            if (base.ContainsKey(key) &&
                base[key] != null)
            {
                value = (T)base[key];
            }

            return value;
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// <see cref="http://stackoverflow.com/questions/1207731/how-can-i-deserialize-json-to-a-simple-dictionarystring-string-in-asp-net"/>
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
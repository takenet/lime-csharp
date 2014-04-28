using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Serialization
{
    /// <summary>
    /// Represents a JSON object instance
    /// </summary>
    public sealed class JsonObject : Dictionary<string, object>
    {
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

            if (base.ContainsKey(key))
            {
                value = (T)base[key];
            }

            return value;
        }
    }
}
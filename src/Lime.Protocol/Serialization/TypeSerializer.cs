using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Serialization
{
    public static class TypeSerializer<T> where T : class, new()
    {
        private static Dictionary<string, Action<T, IJsonWriter>> _writePropertyActionDictionary;

        static TypeSerializer()
        {
            _writePropertyActionDictionary = new Dictionary<string, Action<T, IJsonWriter>>();

            var type = typeof(T);

            var properties = type
                .GetProperties()
                .Where(p => p.GetCustomAttribute<DataMemberAttribute>() != null);


            foreach (var property in properties)
            {
                var dataMember = property.GetCustomAttribute<DataMemberAttribute>();
                var memberName = dataMember.Name;

                Action<T, IJsonWriter> writePropertyAction = null;

                if (property.PropertyType.IsArray)
                {                    
                    writePropertyAction = (v, w) =>
                        {
                            var value = property.GetValue(v);
                            if (value != null)
                            {
                                w.WriteArrayProperty(memberName, (IEnumerable)value);
                            }
                        };
                }
                else
                {
                    writePropertyAction = (v, w) => w.WriteProperty(memberName, property.GetValue(v));
                }
                _writePropertyActionDictionary.Add(memberName, writePropertyAction);
                
            }
        }

        public static string Serialize(T value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            using (var writer = new TextJsonWriter())
            {
                Write(value, writer);
                return writer.ToString();
            }
        }

        public static void Write(T value, IJsonWriter writer)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            foreach (var writePropertyActionPar in _writePropertyActionDictionary)
            {
                writePropertyActionPar.Value(value, writer);
            }            
        }


        public static T Deserialize(string json)
        {
            var jsonObject = JsonObject.ParseJson(json);

            T value = new T();

            

            return value;
        }
    }

    public static class TypeSerializer
    {
        private static ConcurrentDictionary<Type, MethodInfo> _typeSerializeMethodDictionary = new ConcurrentDictionary<Type, MethodInfo>();

        public static string Serialize(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            var type = value.GetType();

            MethodInfo serializeMethod;

            if (!_typeSerializeMethodDictionary.TryGetValue(type, out serializeMethod))
            {
                var serializer = typeof(TypeSerializer<>).MakeGenericType(type);
                serializeMethod = serializer.GetMethod("Serialize", BindingFlags.Static | BindingFlags.Public);
                _typeSerializeMethodDictionary.TryAdd(type, serializeMethod);
            }

            return (string)serializeMethod.Invoke(null, new[] { value });
        }
    }
}

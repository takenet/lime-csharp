using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Serialization
{
    public static class TypeSerializer<T> where T : class, new()
    {
        private static readonly Action<T, IJsonWriter>[] _serializePropertyActions;
        private static readonly Action<T, JsonObject>[] _deserializePropertyActions;

        static TypeSerializer()
        {           
            var type = typeof(T);

            var properties = type
                .GetProperties()
                .Where(p => p.GetCustomAttribute<DataMemberAttribute>() != null)
                .ToArray();

            _serializePropertyActions = new Action<T, IJsonWriter>[properties.Length];

            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                var dataMember = property.GetCustomAttribute<DataMemberAttribute>();
                var memberName = dataMember.Name;
                var emitDefaultValue = dataMember.EmitDefaultValue;
                var defaultValue = property.PropertyType.GetDefaultValue();
                var getValueFunc = TypeUtil.BuildGetAccessor(property.GetGetMethod());

                Action<T, IJsonWriter> writePropertyAction = null;

                if (property.PropertyType.IsArray)
                {                    
                    writePropertyAction = (v, w) =>
                        {
                            var value = getValueFunc(v);
                            if (value != null)
                            {
                                w.WriteArrayProperty(memberName, (IEnumerable)value);
                            }
                        };                    
                }
                else
                {
                    writePropertyAction = (v, w) =>
                    {
                        var value = getValueFunc(v);
                        if (emitDefaultValue || (value != null && !value.Equals(defaultValue)))
                        {
                            w.WriteProperty(memberName, value);
                        }
                    };
                }

                _serializePropertyActions[i] = writePropertyAction;                
            }
        }

        public static string Serialize(T value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            using (var writer = new TextJsonWriter2())
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

            for (int i = 0; i < _serializePropertyActions.Length; i++)
            {
                _serializePropertyActions[i](value, writer);
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
        private static ConcurrentDictionary<Type, Func<object, string>> _serializeFuncDictionary = new ConcurrentDictionary<Type, Func<object, string>>();
        private static ConcurrentDictionary<Type, Action<object, IJsonWriter>> _writeActionDictionary = new ConcurrentDictionary<Type, Action<object, IJsonWriter>>();

        public static string Serialize(object value)
        {            
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            var type = value.GetType();

            Func<object, string> serializeFunc;

            if (!_serializeFuncDictionary.TryGetValue(type, out serializeFunc))
            {
                var serializer = typeof(TypeSerializer<>).MakeGenericType(type);
                var serializeFuncType = typeof(Func<,>).MakeGenericType(type, typeof(string));
                var method = serializer.GetMethod("Serialize", BindingFlags.Static | BindingFlags.Public);
                var genericSerializeFunc = Delegate.CreateDelegate(serializeFuncType, method);
                var convertSerializeDelegateMethod = typeof(TypeSerializer)
                    .GetMethod("ConvertSerializeDelegate", BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod(type);

                serializeFunc = (Func<object, string>)convertSerializeDelegateMethod.Invoke(null, new[] { genericSerializeFunc });
                _serializeFuncDictionary.TryAdd(type, serializeFunc);
            }

            return serializeFunc(value);
        }       

        public static void Write(object value, IJsonWriter writer)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            var type = value.GetType();
            
            Action<object, IJsonWriter> writeAction;
            if (!_writeActionDictionary.TryGetValue(type, out writeAction))
            {
                var serializer = typeof(TypeSerializer<>).MakeGenericType(type);
                var writeActionType = typeof(Action<,>).MakeGenericType(type, typeof(IJsonWriter));
                var method = serializer.GetMethod("Write", BindingFlags.Static | BindingFlags.Public);
                var genericWriteAction = Delegate.CreateDelegate(writeActionType, method);
                var convertWriteDelegateMethod = typeof(TypeSerializer)
                    .GetMethod("ConvertWriteDelegate", BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod(type);
                writeAction = (Action<object, IJsonWriter>)convertWriteDelegateMethod.Invoke(null, new[] { genericWriteAction });
                _writeActionDictionary.TryAdd(type, writeAction);
            }

            writeAction(value, writer);
        }

        private static Func<object, string> ConvertSerializeDelegate<T>(Func<T, string> func)
        {
            return (object p) => func((T)p);
        }

        private static Action<object, IJsonWriter> ConvertWriteDelegate<T>(Action<T, IJsonWriter> action)
        {
            return (object p1, IJsonWriter p2) => action((T)p1, p2);
        }


    }
}
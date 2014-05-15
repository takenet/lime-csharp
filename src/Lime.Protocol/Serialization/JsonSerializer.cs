using Lime.Protocol.Security;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Serialization
{
    /// <summary>
    /// Provides JSON serialization support
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class JsonSerializer<T> where T : class, new()
    {
        #region Private Fields

        private static readonly Action<T, IJsonWriter>[] _serializePropertyActions;
        private static readonly Action<T, JsonObject>[] _deserializePropertyActions;

        #endregion

        #region Constructor

        static JsonSerializer()
        {
            var type = typeof(T);

            var properties = type
                .GetProperties()
                .Where(p => p.GetCustomAttribute<DataMemberAttribute>() != null)
                .ToArray();

            _serializePropertyActions = new Action<T, IJsonWriter>[properties.Length];
            _deserializePropertyActions = new Action<T, JsonObject>[properties.Length];

            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                var dataMemberAttribute = property.GetCustomAttribute<DataMemberAttribute>();
                var memberName = dataMemberAttribute.Name;
                var emitDefaultValue = dataMemberAttribute.EmitDefaultValue;
                
                object defaultValue;
                var defaultValueAttribute = property.GetCustomAttribute<DefaultValueAttribute>();
                if (defaultValueAttribute != null)
                {
                    defaultValue = defaultValueAttribute.Value;
                }
                else
                {
                    defaultValue = property.PropertyType.GetDefaultValue();
                }                
                var propertyType = property.PropertyType;

                // Serialization
                var getMethod = property.GetGetMethod();
                if (getMethod != null)
                {
                    _serializePropertyActions[i] = GetPropertySerializationAction(propertyType, getMethod, memberName, defaultValue, emitDefaultValue);
                }

                // Deserialization                
                var setMethod = property.GetSetMethod();
                if (setMethod != null)
                {
                    _deserializePropertyActions[i] = GetPropertyDeserializationAction(propertyType, setMethod, memberName, defaultValue);
                }
            }
        }

        #endregion

        #region Private Methods

        private static Action<T, IJsonWriter> GetPropertySerializationAction(Type propertyType, MethodInfo getMethod, string memberName, object defaultValue, bool emitDefaultValue)
        {
            var getFunc = TypeUtil.BuildGetAccessor(getMethod);

            Action<T, IJsonWriter> serializePropertyAction = null;

            if (propertyType.IsArray)
            {
                serializePropertyAction = (v, w) =>
                {
                    var value = getFunc(v);
                    if (value != null)
                    {
                        w.WriteArrayProperty(memberName, (IEnumerable)value);
                    }
                };
            }
            else
            {
                serializePropertyAction = (v, w) =>
                {
                    var value = getFunc(v);
                    if (emitDefaultValue || (value != null && !value.Equals(defaultValue)))
                    {
                        w.WriteProperty(memberName, value);
                    }
                };
            }
            return serializePropertyAction;
        }

        private static Action<T, JsonObject> GetPropertyDeserializationAction(Type propertyType, MethodInfo setMethod, string memberName, object defaultValue)
        {
            var setFunc = TypeUtil.BuildSetAccessor(setMethod);

            Action<T, JsonObject> deserializePropertyAction = null;
            var isNullable = false;

            if (propertyType.IsGenericType &&
                propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                propertyType = propertyType.GetGenericArguments().First();
                isNullable = true;
            };

            if (propertyType.IsArray)
            {
                propertyType = propertyType.GetElementType();

                if (propertyType.IsEnum)
                {
                    deserializePropertyAction = (v, j) =>
                    {
                        var value = j.GetEnumArrayOrNull(propertyType, memberName) ?? defaultValue;
                        if (value != null)
                        {
                            setFunc(v, value);
                        }
                    };
                }
                else if (TypeUtil.IsKnownType(propertyType))
                {
                    // In this case, the dictionary has a JsonObject entry
                    // for the property, so it must be parsed
                    deserializePropertyAction = (v, j) =>
                    {
                        if (j.ContainsKey(memberName) &&
                            j[memberName] is IEnumerable)
                        {
                            var value = j.GetArrayOrNull(propertyType, memberName, i => JsonSerializer.ParseJson(propertyType, (JsonObject)i));

                            if (value != null)
                            {
                                setFunc(v, value);
                            }
                        }
                    };
                }
                else
                {
                    try
                    {
                        var parseFunc = TypeUtil.GetParseFuncForType(propertyType);

                        deserializePropertyAction = (v, j) =>
                        {
                            var value = j.GetArrayOrNull(propertyType, memberName, i => parseFunc((string)i));
                            if (value != null)
                            {
                                setFunc(v, value);
                            }
                        };
                    }
                    catch (ArgumentException)
                    {
                        throw new NotSupportedException(string.Format("The type '{0}' of the property '{1}' is not supported", propertyType, memberName));
                    }
                }
            }
            else if (propertyType.IsEnum)
            {
                deserializePropertyAction = (v, j) =>
                {
                    var value = j.GetEnumValueOrNull(propertyType, memberName) ?? defaultValue;
                    if (isNullable || value != null)
                    {
                        setFunc(v, value);
                    }
                };
            }
            else if (propertyType == typeof(int))
            {
                deserializePropertyAction = (v, j) =>
                {
                    var value = j.GetValueOrNull<int>(memberName) ?? defaultValue;
                    if (isNullable || value != null)
                    {
                        setFunc(v, value);
                    }
                };
            }
            else if (propertyType == typeof(bool))
            {
                deserializePropertyAction = (v, j) =>
                {
                    var value = j.GetValueOrNull<bool>(memberName) ?? defaultValue;
                    if (isNullable || value != null)
                    {
                        setFunc(v, value);
                    }
                };
            }
            else if (propertyType == typeof(long))
            {
                deserializePropertyAction = (v, j) =>
                {
                    var value = j.GetValueOrNull<long>(memberName) ?? defaultValue;
                    if (isNullable || value != null)
                    {
                        setFunc(v, value);
                    }
                };
            }
            else if (propertyType == typeof(string))
            {
                deserializePropertyAction = (v, j) =>
                {
                    var value = j.GetValueOrDefault<string>(memberName) ?? defaultValue;
                    if (value != null)
                    {
                        setFunc(v, value);
                    }
                };
            }
            else if (typeof(IDictionary<string, string>).IsAssignableFrom(propertyType))
            {
                // Metadata property
                deserializePropertyAction = (v, j) =>
                {
                    var value = j.GetValueOrDefault<IDictionary<string, string>>(memberName, d => ((IDictionary<string, object>)d).ToDictionary(e => e.Key, e => (string)e.Value));
                    if (value != null)
                    {
                        setFunc(v, value);
                    }
                };
            }
            else if (propertyType.IsAbstract)
            {
                if (propertyType == typeof(Document))
                {
                    // Determine the type of the property using the
                    // envelope content/resource Mime Type
                    deserializePropertyAction = (v, j) =>
                    {
                        if (j.ContainsKey(Message.TYPE_KEY) &&
                            j[Message.TYPE_KEY] is string)
                        {
                            var mediaType = MediaType.Parse((string)j[Message.TYPE_KEY]);                            

                            JsonObject propertyJsonObject = null;
                            if (j.ContainsKey(memberName) &&
                                j[memberName] is JsonObject)
                            {
                                propertyJsonObject = (JsonObject)j[memberName];
                            }
                            
                            object value;
                            Type concreteType;                            

                            if (TypeUtil.TryGetTypeForMediaType(mediaType, out concreteType))
                            {                                
                                if (propertyJsonObject != null)
                                {
                                    value = JsonSerializer.ParseJson(concreteType, propertyJsonObject);
                                }
                                else
                                {
                                    value = TypeUtil.CreateInstance(concreteType);
                                }
                            }
                            else if (propertyJsonObject != null)
                            {
                                value = new JsonDocument(propertyJsonObject, mediaType);
                            }
                            else
                            {
                                value = new JsonDocument(mediaType);
                            }

                            if (value != null)
                            {
                                setFunc(v, value);
                            }
                        }
                    };
                }
                else if (propertyType == typeof(Authentication))
                {
                    // Determine the type of the property using the
                    // session Authentication property
                    deserializePropertyAction = (v, j) =>
                    {
                        if (j.ContainsKey(memberName) &&
                            j[memberName] is JsonObject &&
                            j.ContainsKey(Session.SCHEME_KEY) &&
                            j[Session.SCHEME_KEY] is string)
                        {
                            var propertyJsonObject = (JsonObject)j[memberName];
                            var scheme = TypeUtil.GetEnumValue<AuthenticationScheme>((string)j[Session.SCHEME_KEY]);
                            Type concreteType;

                            if (TypeUtil.TryGetTypeForAuthenticationScheme(scheme, out concreteType))
                            {
                                var value = JsonSerializer.ParseJson(concreteType, propertyJsonObject);
                                if (value != null)
                                {
                                    setFunc(v, value);
                                }
                            }
                        }
                    };
                }
                else
                {
                    throw new NotSupportedException(string.Format("The type '{0}' of the property '{1}' is not supported", propertyType, memberName));
                }
            }
            else if (TypeUtil.IsKnownType(propertyType))
            {
                // In this case, the dictionary has a JsonObject entry
                // for the property, so it must be parsed
                deserializePropertyAction = (v, j) =>
                {
                    if (j.ContainsKey(memberName) &&
                        j[memberName] is JsonObject)
                    {
                        var propertyJsonObject = (JsonObject)j[memberName];
                        var value = JsonSerializer.ParseJson(propertyType, propertyJsonObject) ?? defaultValue;
                        if (isNullable || value != null)
                        {
                            setFunc(v, value);
                        }
                    }
                };                
            }
            else
            {
                // Checks if the type has a static Parse function
                try
                {
                    var parseFunc = TypeUtil.GetParseFuncForType(propertyType);

                    deserializePropertyAction = (v, j) =>
                    {
                        var value = j.GetValueOrNull(memberName, parseFunc) ?? defaultValue;
                        if (isNullable || value != null)
                        {
                            setFunc(v, value);
                        }
                    };
                }
                catch (ArgumentException)
                {
                    throw new NotSupportedException(string.Format("The type '{0}' of the property '{1}' is not supported", propertyType, memberName));
                }
            }

            return deserializePropertyAction;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">value</exception>
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

        /// <summary>
        /// Writes the specified value
        /// to the IJsonWriter.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="writer">The writer.</param>
        /// <exception cref="System.ArgumentNullException">
        /// value
        /// or
        /// writer
        /// </exception>
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
                var serializePropertyAction = _serializePropertyActions[i];
                if (serializePropertyAction != null)
                {
                    _serializePropertyActions[i](value, writer);
                }
            }
        }

        /// <summary>
        /// Deserializes the specified json.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">json</exception>
        public static T Deserialize(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException("json");
            }

            var jsonObject = JsonObject.ParseJson(json);

            return ParseJson(jsonObject);
        }

        /// <summary>
        /// Parses a JsonObject
        /// to a type.
        /// </summary>
        /// <param name="jsonObject">The json object.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">jsonObject</exception>
        public static T ParseJson(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                throw new ArgumentNullException("jsonObject");
            }

            T value = new T();

            for (int i = 0; i < _deserializePropertyActions.Length; i++)
            {
                var deserializePropertyAction = _deserializePropertyActions[i];
                if (deserializePropertyAction != null)
                {
                    deserializePropertyAction(value, jsonObject);
                }
            }

            return value;
        }

        #endregion
    }

    /// <summary>
    /// Provides JSON serialization support
    /// </summary>
    public static class JsonSerializer
    {
        #region Private Fields

        private static ConcurrentDictionary<Type, Func<object, string>> _serializeFuncDictionary;
        private static ConcurrentDictionary<Type, Action<object, IJsonWriter>> _writeActionDictionary;
        private static ConcurrentDictionary<Type, Func<string, object>> _deserializeFuncDictionary;
        private static ConcurrentDictionary<Type, Func<JsonObject, object>> _parseJsonFuncDictionary;

        #endregion

        #region Constructor

        static JsonSerializer()
        {
            _serializeFuncDictionary = new ConcurrentDictionary<Type, Func<object, string>>();
            _writeActionDictionary = new ConcurrentDictionary<Type, Action<object, IJsonWriter>>();
            _deserializeFuncDictionary = new ConcurrentDictionary<Type, Func<string, object>>();
            _parseJsonFuncDictionary = new ConcurrentDictionary<Type, Func<JsonObject, object>>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">value</exception>
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
                var serializer = typeof(JsonSerializer<>).MakeGenericType(type);
                var serializeFuncType = typeof(Func<,>).MakeGenericType(type, typeof(string));
                var method = serializer.GetMethod("Serialize", BindingFlags.Static | BindingFlags.Public);
                var genericSerializeFunc = Delegate.CreateDelegate(serializeFuncType, method);
                var serializeFuncAdapterMethod = typeof(JsonSerializer)
                    .GetMethod("SerializeFuncAdapter", BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod(type);

                try
                {
                    serializeFunc = (Func<object, string>)serializeFuncAdapterMethod.Invoke(null, new[] { genericSerializeFunc });
                    _serializeFuncDictionary.TryAdd(type, serializeFunc);
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
            }

            return serializeFunc(value);
        }

        /// <summary>
        /// Writes the specified value
        /// to the JSON writer.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="writer">The writer.</param>
        /// <exception cref="System.ArgumentNullException">value</exception>
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
                var serializer = typeof(JsonSerializer<>).MakeGenericType(type);
                var writeActionType = typeof(Action<,>).MakeGenericType(type, typeof(IJsonWriter));
                var method = serializer.GetMethod("Write", BindingFlags.Static | BindingFlags.Public);
                var genericWriteAction = Delegate.CreateDelegate(writeActionType, method);
                var writeFuncAdapterMethod = typeof(JsonSerializer)
                    .GetMethod("WriteFuncAdapter", BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod(type);
                try
                {
                    writeAction = (Action<object, IJsonWriter>)writeFuncAdapterMethod.Invoke(null, new[] { genericWriteAction });
                    _writeActionDictionary.TryAdd(type, writeAction);
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
            }

            writeAction(value, writer);
        }

        /// <summary>
        /// Deserializes the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="json">The json.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// type
        /// or
        /// type
        /// </exception>
        public static object Deserialize(Type type, string json)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentNullException("type");
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Parses the json.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="jsonObject">The json object.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// type
        /// or
        /// jsonObject
        /// </exception>
        public static object ParseJson(Type type, JsonObject jsonObject)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (jsonObject == null)
            {
                throw new ArgumentNullException("jsonObject");
            }

            Func<JsonObject, object> parseJsonFunc;
            if (!_parseJsonFuncDictionary.TryGetValue(type, out parseJsonFunc))
            {
                var serializer = typeof(JsonSerializer<>).MakeGenericType(type);
                var parseJsonFuncType = typeof(Func<,>).MakeGenericType(typeof(JsonObject), type);
                var method = serializer.GetMethod("ParseJson", BindingFlags.Static | BindingFlags.Public);
                var genericParseJsonFunc = Delegate.CreateDelegate(parseJsonFuncType, method);
                var parseJsonAdapterMethod = typeof(JsonSerializer)
                    .GetMethod("ParseJsonFuncAdapter", BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod(type);
                try
                {
                    parseJsonFunc = (Func<JsonObject, object>)parseJsonAdapterMethod.Invoke(null, new[] { genericParseJsonFunc });
                    _parseJsonFuncDictionary.TryAdd(type, parseJsonFunc);
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }

            }

            return parseJsonFunc(jsonObject);
        }

        #endregion

        #region Private Methods

        private static Func<object, string> SerializeFuncAdapter<T>(Func<T, string> func)
        {
            return (object p) => func((T)p);
        }

        private static Action<object, IJsonWriter> WriteFuncAdapter<T>(Action<T, IJsonWriter> action)
        {
            return (object p1, IJsonWriter p2) => action((T)p1, p2);
        }

        private static Func<JsonObject, object> ParseJsonFuncAdapter<T>(Func<JsonObject, T> parseJsonFunc)
        {
            return (j) => (object)parseJsonFunc(j);
        }

        #endregion
    }
}
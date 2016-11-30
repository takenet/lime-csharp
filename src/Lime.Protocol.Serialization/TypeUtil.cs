using System.IO;
using Lime.Protocol.Security;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Lime.Protocol.Serialization
{
    /// <summary>
    /// Provides metadata information about the types.
    /// </summary>
    public static class TypeUtil
    {
        private static readonly ConcurrentDictionary<MediaType, Type> _documentMediaTypeDictionary;
        private static readonly ConcurrentDictionary<AuthenticationScheme, Type> _authenticationSchemeDictionary;
        private static readonly ConcurrentDictionary<Type, IDictionary<string, object>> _enumTypeValueDictionary;
        private static readonly ConcurrentDictionary<Type, Func<string, object>> _typeParseFuncDictionary;
        private static readonly HashSet<Type> _dataContractTypes;

        public static readonly Func<AssemblyName, bool> IgnoreSystemAndMicrosoftAssembliesFilter =
            a => !a.FullName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) &&
                 !a.FullName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase);

        private static readonly object _loadAssembliesSyncRoot = new object();
        private static bool _referencedAssembliesLoaded;

        static TypeUtil()
        {
            _documentMediaTypeDictionary = new ConcurrentDictionary<MediaType, Type>();
            _authenticationSchemeDictionary = new ConcurrentDictionary<AuthenticationScheme, Type>();
            _enumTypeValueDictionary = new ConcurrentDictionary<Type, IDictionary<string, object>>();
            _typeParseFuncDictionary = new ConcurrentDictionary<Type, Func<string, object>>();
            _dataContractTypes = new HashSet<Type>();

            // Caches the known type (types decorated with DataContract in all loaded assemblies)
            //foreach (var type in GetAllLoadedTypes().Where(t => t.GetTypeInfo().GetCustomAttribute<DataContractAttribute>() != null))
            //{
            //    RegisterType(type);
            //}
        }

        /// <summary>
        /// Gets the Parse static 
        /// method of a Type as 
        /// a func
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Func<string, T> GetParseFunc<T>()
        {
            var type = typeof(T);

            var parseMethod = typeof(T)
                .GetTypeInfo()
                .GetMethod("Parse", BindingFlags.Static | BindingFlags.Public);

            if (parseMethod == null)
            {
                throw new ArgumentException($"The type '{type}' doesn't contains a static 'Parse' method");
            }

            if (parseMethod.ReturnType != type)
            {
                throw new ArgumentException("The Parse method has an invalid return type");
            }

            var parameters = parseMethod.GetParameters();
            if (parameters.Length != 1)
            {
                throw new ArgumentException($"The type '{type}' 'Parse' must accept only one argument");
            }

            if (parameters[0].ParameterType != typeof(string))
            {
                throw new ArgumentException($"The type '{type}' 'Parse' argument should be a string");
            }

            var parseFuncType = typeof(Func<,>).MakeGenericType(typeof(string), type);
            return (Func<string, T>) parseMethod.CreateDelegate(parseFuncType);
        }

        /// <summary>
        /// Gets the Parse static 
        /// method of a Type as 
        /// a func
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Func<string, object> GetParseFuncForType(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));            
            Func<string, object> parseFunc;

            if (!_typeParseFuncDictionary.TryGetValue(type, out parseFunc))
            {
                try
                {
                    var getParseFuncMethod = typeof(TypeUtil)
                        .GetMethod("GetParseFunc", BindingFlags.Static | BindingFlags.Public)
                        .MakeGenericMethod(type);

                    var genericGetParseFunc = getParseFuncMethod.Invoke(null, null);

                    var parseFuncAdapterMethod = typeof(TypeUtil)
                        .GetMethod("ParseFuncAdapter", BindingFlags.Static | BindingFlags.NonPublic)
                        .MakeGenericMethod(type);

                    parseFunc = (Func<string, object>)parseFuncAdapterMethod.Invoke(null, new[] { genericGetParseFunc });
                    _typeParseFuncDictionary.TryAdd(type, parseFunc);
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
            }

            return parseFunc; 
        }

        private static Func<string, object> ParseFuncAdapter<T>(Func<string, T> parseFunc)
        {
            return (s) => (object)parseFunc(s);
        }


        /// <summary>
        /// Try parses the string to 
        /// a object of the specified type
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryParseString(string value, Type type, out object result)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (type == null) throw new ArgumentNullException(nameof(type));
            

            if (type.GetTypeInfo().IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0];
            }

            if (type == typeof(string))
            {
                result = value;
                return true;
            }
            else if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var arrayValues = value.Split(';');
                
                var resultArray = Array.CreateInstance(elementType, arrayValues.Length);

                for (int i = 0; i < arrayValues.Length; i++)
                {
                    var arrayValue = arrayValues[i];
                    object resultArrayElement;

                    if (TryParseString(arrayValue, elementType, out resultArrayElement))
                    {
                        resultArray.SetValue(resultArrayElement, i);
                    }
                    else
                    {
                        result = null;
                        return false;
                    }                    
                }

                result = resultArray;
                return true;
            }
            else if (type.GetTypeInfo().IsEnum)
            {
                try
                {
                    result = Enum.Parse(type, value, true);
                    return true;
                }
                catch (ArgumentException)
                {
                    result = null;
                    return false;
                }
            }
            else
            {
                try
                {
                    var parseFunc = GetParseFuncForType(type);
                    result = parseFunc(value);
                    return true;
                }
                catch
                {
                    result = null;
                    return false;
                }
            }
        }

        /// <summary>
        /// Tries to get the registered type for
        /// the specified media type.
        /// </summary>
        /// <param name="mediaType"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool TryGetTypeForMediaType(MediaType mediaType, out Type type)
        {
            return _documentMediaTypeDictionary.TryGetValue(mediaType, out type);            
        }

        public static bool TryGetTypeForAuthenticationScheme(AuthenticationScheme scheme, out Type type)
        {
            return _authenticationSchemeDictionary.TryGetValue(scheme, out type);
        }

        /// <summary>
        /// Gets a cached value 
        /// for a enum item
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="enumName"></param>
        /// <returns></returns>
        public static TEnum ParseEnum<TEnum>(string enumName) where TEnum : struct
        {
            var enumType = typeof(TEnum);

            if (!enumType.GetTypeInfo().IsEnum)
            {
                throw new ArgumentException("Type is not enum");
            }

            IDictionary<string, object> memberValueDictionary;

            if (!_enumTypeValueDictionary.TryGetValue(enumType, out memberValueDictionary))
            {
                // If not cached, try by reflection
                TEnum result;

                if (Enum.TryParse<TEnum>(enumName, true, out result))
                {
                    return result;
                }
                else
                {
                    throw new ArgumentException("Unknown enum type");
                }
            }

            object value;

            if (!memberValueDictionary.TryGetValue(enumName.ToLowerInvariant(), out value))
            {
                throw new ArgumentException("Invalid enum member name");
            }            

            return (TEnum)value;
        }

        /// <summary>
        /// Gets a cached value 
        /// for a enum item
        /// </summary>
        /// <param name="enumType"></param>
        /// <param name="enumName"></param>
        /// <returns></returns>
        public static object ParseEnum(Type enumType, string enumName)
        {
            IDictionary<string, object> memberValueDictionary;

            if (!_enumTypeValueDictionary.TryGetValue(enumType, out memberValueDictionary))
            {                
                throw new ArgumentException("Unknown enum type");                
            }

            object value;

            if (!memberValueDictionary.TryGetValue(enumName.ToLowerInvariant(), out value))
            {
                throw new ArgumentException("Invalid enum member name");
            }

            return value;
        }

        /// <summary>
        /// Gets the assembly enums decorated
        /// with the DataContract attribute
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Type> GetEnumTypes()
        {
            return _enumTypeValueDictionary.Keys;
        }

        /// <summary>
        /// Indicates if the type is a
        /// protocol JSON type, decorated
        /// with the DataContract attribute
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsDataContractType(Type type)
        {
            return _dataContractTypes.Contains(type);
        }

        /// <summary>
        /// Create and register a document type for serialization support.
        /// </summary>
        /// <typeparam name="TDocument"></typeparam>
        public static void RegisterDocument<TDocument>() 
            where TDocument : Document, new()
        {
            RegisterType(typeof(TDocument));
        }

        /// <summary>
        /// Gets the default value for 
        /// the Type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetDefaultValue<T>()
        {
            // We want an Func<T> which returns the default.
            // Create that expression here.
            Expression<Func<T>> e = Expression.Lambda<Func<T>>(
                // The default value, always get what the *code* tells us.
                Expression.Default(typeof(T))
            );

            // Compile and return the value.
            return e.Compile()();
        }

                /// <summary>
        /// Build a delegate to
        /// get a property value
        /// of a class
        /// </summary>
        /// <a href="http://stackoverflow.com/questions/10820453/reflection-performance-create-delegate-properties-c"/>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public static Func<object, object> BuildGetAccessor(PropertyInfo propertyInfo)
        {
            return BuildGetAccessor(propertyInfo.GetGetMethod());
        }

        /// <summary>
        /// Build a delegate to
        /// get a property value
        /// of a class
        /// </summary>
        /// <a href="http://stackoverflow.com/questions/10820453/reflection-performance-create-delegate-properties-c"/>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public static Func<object, object> BuildGetAccessor(MethodInfo methodInfo)
        {
            if (methodInfo == null) throw new ArgumentNullException(nameof(methodInfo));
                        
            var obj = Expression.Parameter(typeof(object), "o");

            Expression<Func<object, object>> expr =
                Expression.Lambda<Func<object, object>>(
                    Expression.Convert(
                        Expression.Call(
                            Expression.Convert(obj, methodInfo.DeclaringType),
                            methodInfo),
                        typeof(object)),
                    obj);

            return expr.Compile();
        }

                /// <summary>
        /// Build a delegate to
        /// set a property value
        /// of a class
        /// </summary>
        /// <a href="http://stackoverflow.com/questions/10820453/reflection-performance-create-delegate-properties-c"/>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public static Action<object, object> BuildSetAccessor(PropertyInfo propertyInfo)
        {
            return BuildSetAccessor(propertyInfo.GetSetMethod());
        }

        /// <summary>
        /// Build a delegate to
        /// set a property value
        /// of a class
        /// </summary>
        /// <a href="http://stackoverflow.com/questions/10820453/reflection-performance-create-delegate-properties-c"/>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public static Action<object, object> BuildSetAccessor(MethodInfo methodInfo)
        {
            if (methodInfo == null) throw new ArgumentNullException(nameof(methodInfo));
            
            var obj = Expression.Parameter(typeof(object), "o");
            var value = Expression.Parameter(typeof(object));

            Expression<Action<object, object>> expr =
                Expression.Lambda<Action<object, object>>(
                    Expression.Call(
                        Expression.Convert(obj, methodInfo.DeclaringType),
                        methodInfo,
                        Expression.Convert(value, methodInfo.GetParameters()[0].ParameterType)),
                    obj,
                    value);

            return expr.Compile();
        }


        /// <summary>
        /// Creates an instance
        /// of the type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object CreateInstance(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));           
            return Activator.CreateInstance(type);
        }


        private static void RegisterType(Type type)
        {
            if (type.GetTypeInfo().GetCustomAttribute<DataContractAttribute>() != null)
            {
                _dataContractTypes.Add(type);
            }

            if (!type.GetTypeInfo().IsAbstract)
            {
                // Caches the documents (contents and resources)
                if (typeof(Document).IsAssignableFrom(type))
                {
                    var document = Activator.CreateInstance(type) as Document;
                    if (document != null)
                    {
                        _documentMediaTypeDictionary[document.GetMediaType()] = type;
                    }
                }

                // Caches the Authentication schemes
                if (typeof(Authentication).IsAssignableFrom(type))
                {
                    var authentication = Activator.CreateInstance(type) as Authentication;
                    if (authentication != null)
                    {
                        _authenticationSchemeDictionary[authentication.GetAuthenticationScheme()] = type;
                    }
                }
            }

            // Caches the enums
            if (type.GetTypeInfo().IsEnum)
            {
                var enumNames = Enum.GetNames(type);
                var memberValueDictionary = new Dictionary<string, object>();

                foreach (var enumName in enumNames)
                {
                    memberValueDictionary[enumName.ToLowerInvariant()] = Enum.Parse(type, enumName);
                }
                _enumTypeValueDictionary[type] = memberValueDictionary;
            }
        }
    }
}

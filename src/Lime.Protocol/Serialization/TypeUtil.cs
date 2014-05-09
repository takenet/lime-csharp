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

namespace Lime.Protocol.Serialization
{
    /// <summary>
    /// Provides metadata information 
    /// about the protocol types
    /// </summary>
    public static class TypeUtil
    {
        private static IDictionary<MediaType, Type> _documentMediaTypeDictionary;
        private static IDictionary<AuthenticationScheme, Type> _authenticationSchemeDictionary;        
        private static IDictionary<Type, Dictionary<string, object>> _enumTypeValueDictionary;
        private static IDictionary<Type, Delegate> _factoryMethodDictionary;
        private static ConcurrentDictionary<Type, Func<string, object>> _typeParseFuncDictionary;
        private static HashSet<Type> _protocolTypes;

        private static object _syncRoot = new object();
       
        static TypeUtil()
        {
            _documentMediaTypeDictionary = new Dictionary<MediaType, Type>();
            _authenticationSchemeDictionary = new Dictionary<AuthenticationScheme, Type>();
            _factoryMethodDictionary = new Dictionary<Type, Delegate>();
            _enumTypeValueDictionary = new Dictionary<Type, Dictionary<string, object>>();
            _typeParseFuncDictionary = new ConcurrentDictionary<Type, Func<string, object>>();
            _protocolTypes = null;
#if !PCL
            _protocolTypes = new HashSet<Type>();            
            foreach (var protocolType in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<DataContractAttribute>() != null))
            {
                _protocolTypes.Add(protocolType);
            }
                

            var documentTypes = _protocolTypes
                .Where(t => !t.IsAbstract && typeof(Document).IsAssignableFrom(t));

            foreach (var documentType in documentTypes)
            {
                var document = Activator.CreateInstance(documentType) as Document;

                if (document != null)
                {
                    _documentMediaTypeDictionary.Add(document.GetMediaType(), documentType);
                }
            }

            var authenticationTypes = _protocolTypes
                .Where(t => !t.IsAbstract && typeof(Authentication).IsAssignableFrom(t));

            foreach (var authenticationType in authenticationTypes)
            {
                var authentication = Activator.CreateInstance(authenticationType) as Authentication;

                if (authentication != null)
                {
                    _authenticationSchemeDictionary.Add(authentication.GetAuthenticationScheme(), authenticationType);
                }
            }

            var enumTypes = _protocolTypes
                .Where(t => t.IsEnum);

            foreach (var enumType in enumTypes)
            {
                var enumNames = Enum.GetNames(enumType);

                var memberValueDictionary = new Dictionary<string, object>();

                foreach (var enumName in enumNames)
                {
                    memberValueDictionary.Add(enumName.ToLowerInvariant(), Enum.Parse(enumType, enumName));
                }
                _enumTypeValueDictionary.Add(enumType, memberValueDictionary);
            }

#else
            // TODO: Load types
#endif
        }

        public static Func<string, T> GetParseFunc<T>()
        {
            var type = typeof(T);

            var parseMethod = typeof(T)
                .GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string) }, null);

            if (parseMethod == null)
            {
                throw new ArgumentException(string.Format("The type '{0}' doesn't contains a static 'Parse' method", type));
            }

            if (parseMethod.ReturnType != type)
            {
                throw new ArgumentException("The Parse method has an invalid return type");
            }

            var parseFuncType = typeof(Func<,>).MakeGenericType(typeof(string), type);

            return (Func<string, T>)Delegate.CreateDelegate(parseFuncType, parseMethod);
        }
        

        public static Func<string, object> GetParseFuncForType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            Func<string, object> parseFunc;

            if (!_typeParseFuncDictionary.TryGetValue(type, out parseFunc))
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

            return parseFunc; 
        }

        private static Func<string, object> ParseFuncAdapter<T>(Func<string, T> parseFunc)
        {
            return (s) => (object)parseFunc(s);
        }

        public static bool TryGetTypeForMediaType(MediaType mediaType, out Type type)
        {
            return _documentMediaTypeDictionary.TryGetValue(mediaType, out type);            
        }

        public static bool TryGetTypeForAuthenticationScheme(AuthenticationScheme scheme, out Type type)
        {
            return _authenticationSchemeDictionary.TryGetValue(scheme, out type);
        }

        public static TEnum GetEnumValue<TEnum>(string enumName) where TEnum : struct
        {
            var enumType = typeof(TEnum); 
            Dictionary<string, object> memberValueDictionary;

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

        public static object GetEnumValue(Type enumType, string enumName)
        {
            Dictionary<string, object> memberValueDictionary;

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
        public static bool IsProtocolType(Type type)
        {
            return _protocolTypes.Contains(type);
        }

        public static IEnumerable<Type> GetProtocolTypes()
        {
            return _protocolTypes;
        }

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
        /// Gets a delegate to the
        /// static factory method of 
        /// the type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Delegate GetFactoryDelegate(Type type)
        {
            Delegate factoryDelegate;


#if !PCL
            if (!_factoryMethodDictionary.TryGetValue(type, out factoryDelegate))
            {
                lock (_syncRoot)
                {
                    if (!_factoryMethodDictionary.TryGetValue(type, out factoryDelegate))
                    {
                        var fromDictionaryMethod = type
                            .GetMethods(BindingFlags.Static | BindingFlags.Public)
                            .Where(m => m.GetCustomAttribute<FactoryAttribute>() != null)
                            .FirstOrDefault();

                        if (fromDictionaryMethod == null)
                        {
                            throw new ArgumentException("Type doesn't contains a JsonObject factory method");
                        }

                        var delegateType = typeof(Func<JsonObject, object>);

                        factoryDelegate = Delegate.CreateDelegate(delegateType, fromDictionaryMethod);
                        _factoryMethodDictionary.Add(type, factoryDelegate);
                    }
                }
            }
#else
            // TODO: Implements it to PCL
            factoryDelegate = null;
#endif
            return factoryDelegate;
        }

        /// <summary>
        /// http://stackoverflow.com/questions/10820453/reflection-performance-create-delegate-properties-c
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static Func<object, object> BuildGetAccessor(MethodInfo method)
        {
            var obj = Expression.Parameter(typeof(object), "o");

            Expression<Func<object, object>> expr =
                Expression.Lambda<Func<object, object>>(
                    Expression.Convert(
                        Expression.Call(
                            Expression.Convert(obj, method.DeclaringType),
                            method),
                        typeof(object)),
                    obj);

            return expr.Compile();
        }


        public static Action<object, object> BuildSetAccessor(MethodInfo method)
        {
            var obj = Expression.Parameter(typeof(object), "o");
            var value = Expression.Parameter(typeof(object));

            Expression<Action<object, object>> expr =
                Expression.Lambda<Action<object, object>>(
                    Expression.Call(
                        Expression.Convert(obj, method.DeclaringType),
                        method,
                        Expression.Convert(value, method.GetParameters()[0].ParameterType)),
                    obj,
                    value);

            return expr.Compile();
        }

    }
}

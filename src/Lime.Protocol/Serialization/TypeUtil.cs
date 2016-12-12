using Lime.Protocol.Security;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

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
        private static readonly HashSet<Type> _dataContractTypes;

        static TypeUtil()
        {
            _documentMediaTypeDictionary = new ConcurrentDictionary<MediaType, Type>();
            _authenticationSchemeDictionary = new ConcurrentDictionary<AuthenticationScheme, Type>();
            _enumTypeValueDictionary = new ConcurrentDictionary<Type, IDictionary<string, object>>();
            _dataContractTypes = new HashSet<Type>();
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
        /// Registers the documents types for an assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RegisterDocuments(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            var registerDocumentMethod = typeof(TypeUtil).GetMethod(nameof(RegisterDocument));

            var documentTypes = assembly
                .GetTypes()
                .Where(t => !t.GetTypeInfo().IsAbstract && typeof(Document).IsAssignableFrom(t));

            foreach (var type in documentTypes)
            {
                registerDocumentMethod.MakeGenericMethod(type).Invoke(null, null);
            }
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

        internal static void RegisterInternalTypes()
        {
            RegisterDocument<DocumentCollection>();
            RegisterDocument<DocumentContainer>();
            RegisterDocument<IdentityDocument>();
            RegisterDocument<JsonDocument>();

            RegisterType(typeof(GuestAuthentication));
            RegisterType(typeof(KeyAuthentication));
            RegisterType(typeof(PlainAuthentication));
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

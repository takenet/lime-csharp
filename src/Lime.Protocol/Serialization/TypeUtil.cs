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
        private static readonly ConcurrentDictionary<AuthenticationScheme, Type> _authenticationSchemeDictionary;
        private static readonly ConcurrentDictionary<Type, IDictionary<string, object>> _enumTypeValueDictionary;        
        private static readonly HashSet<Type> _dataContractTypes;

        static TypeUtil()
        {
            _authenticationSchemeDictionary = new ConcurrentDictionary<AuthenticationScheme, Type>();
            _enumTypeValueDictionary = new ConcurrentDictionary<Type, IDictionary<string, object>>();
            _dataContractTypes = new HashSet<Type>();
            RegisterInternalTypes();
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

        private static void RegisterInternalTypes()
        {        
            RegisterType(typeof(GuestAuthentication));
            RegisterType(typeof(KeyAuthentication));
            RegisterType(typeof(PlainAuthentication));
            RegisterType(typeof(TransportAuthentication));
            RegisterType(typeof(ExternalAuthentication));
        }

        private static void RegisterType(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (type.GetTypeInfo().GetCustomAttribute<DataContractAttribute>() != null)
            {
                _dataContractTypes.Add(type);
            }

            if (!type.GetTypeInfo().IsAbstract)
            {
                // Caches the Authentication schemes
                if (typeof(Authentication).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                {
                    if (Activator.CreateInstance(type) is Authentication authentication)
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

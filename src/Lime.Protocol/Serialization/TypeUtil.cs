using Lime.Protocol.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private static Dictionary<MediaType, Type> _documentMediaTypeDictionary;
        private static Dictionary<AuthenticationScheme, Type> _authenticationSchemeDictionary;
        
        private static Dictionary<Type, Dictionary<string, object>> _enumTypeValueDictionary;

        private static Dictionary<Type, Delegate> _factoryMethodDictionary;
        private static object _syncRoot = new object();
       
        static TypeUtil()
        {
            _documentMediaTypeDictionary = new Dictionary<MediaType, Type>();
            _authenticationSchemeDictionary = new Dictionary<AuthenticationScheme, Type>();
            _factoryMethodDictionary = new Dictionary<Type, Delegate>();

            _enumTypeValueDictionary = new Dictionary<Type, Dictionary<string, object>>();
#if !PCL
            var assemblyTypes = Assembly
                .GetExecutingAssembly()
                .GetTypes();

            var documentTypes = assemblyTypes
                .Where(t => !t.IsAbstract && typeof(Document).IsAssignableFrom(t));

            foreach (var documentType in documentTypes)
            {
                var document = Activator.CreateInstance(documentType) as Document;

                if (document != null)
                {
                    _documentMediaTypeDictionary.Add(document.GetMediaType(), documentType);
                }
            }

            var authenticationTypes = assemblyTypes
                .Where(t => !t.IsAbstract && typeof(Authentication).IsAssignableFrom(t));

            foreach (var authenticationType in authenticationTypes)
            {
                var authentication = Activator.CreateInstance(authenticationType) as Authentication;

                if (authentication != null)
                {
                    _authenticationSchemeDictionary.Add(authentication.GetAuthenticationScheme(), authenticationType);
                }
            }

            var enumTypes = assemblyTypes
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
            Dictionary<string,object> memberValueDictionary;

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

        public static IEnumerable<Type> GetEnumTypes()
        {
            return _enumTypeValueDictionary.Keys;
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
    }
}

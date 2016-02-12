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
        private static IDictionary<MediaType, Type> _documentMediaTypeDictionary;
        private static IDictionary<AuthenticationScheme, Type> _authenticationSchemeDictionary;
        private static IDictionary<Type, IDictionary<string, object>> _enumTypeValueDictionary;
        private static ConcurrentDictionary<Type, Func<string, object>> _typeParseFuncDictionary;
        private static HashSet<Type> _dataContractTypes;

        public static readonly Func<AssemblyName, bool> IgnoreSystemAndMicrosoftAssembliesFilter =
            a => !a.FullName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) &&
                 !a.FullName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase);

        private static readonly object _loadAssembliesSyncRoot = new object();
        private static bool _referencedAssembliesLoaded;

        #region Constructor

        static TypeUtil()
        {
            _documentMediaTypeDictionary = new Dictionary<MediaType, Type>();
            _authenticationSchemeDictionary = new Dictionary<AuthenticationScheme, Type>();
            _enumTypeValueDictionary = new Dictionary<Type, IDictionary<string, object>>();
            _typeParseFuncDictionary = new ConcurrentDictionary<Type, Func<string, object>>();
            _dataContractTypes = new HashSet<Type>();

            // Caches the known type (types decorated with DataContract in all loaded assemblies)
            foreach (var type in GetAllLoadedTypes().Where(t => t.GetCustomAttribute<DataContractAttribute>() != null))
            {
                AddDataContractType(type);
            }          
        }

        #endregion

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

        /// <summary>
        /// Gets the Parse static 
        /// method of a Type as 
        /// a func
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Func<string, object> GetParseFuncForType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

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
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (type.IsGenericType &&
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
            else if (type.IsEnum)
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

            if (!enumType.IsEnum)
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
        /// CreateAndRegister a document type for serialization support.
        /// </summary>
        /// <typeparam name="TDocument"></typeparam>
        public static void RegisterDocument<TDocument>() 
            where TDocument : Document, new()
        {
            var documentType = typeof(TDocument);

            if (documentType.GetCustomAttribute<DataContractAttribute>() != null)
            {
                AddDataContractType(documentType);
            }
            else
            {
                var document = (Document)CreateInstance(documentType);
                _documentMediaTypeDictionary.Add(document.GetMediaType(), documentType);

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
            if (methodInfo == null)
            {
                throw new ArgumentNullException("methodInfo");
            }

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
            if (methodInfo == null)
            {
                throw new ArgumentNullException("methodInfo");
            }

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

        /// <summary>
        /// Gets all loaded types in the current <see cref="AppDomain"/>, except the ones in the <c>System</c> and <c>Microsoft</c> namespaces.
        /// </summary>
        /// <param name="loadReferences">Load all referenced assemblies before retrieving the types.</param>
        /// <returns></returns>
        public static IEnumerable<Type> GetAllLoadedTypes(bool loadReferences = true)
        {
            if (loadReferences)
            {
                if (!_referencedAssembliesLoaded)
                {
                    lock (_loadAssembliesSyncRoot)
                    {
                        if (!_referencedAssembliesLoaded)
                        {
                            try
                            {
                                LoadAssemblyAndReferences(Assembly.GetExecutingAssembly().GetName(), IgnoreSystemAndMicrosoftAssembliesFilter);
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceError("LIME - An error occurred while loading the referenced assemblies: {0}",
                                    ex.ToString());
                            }
                            finally
                            {
                                _referencedAssembliesLoaded = true;
                            }
                        }
                    }
                }
            }
            return AppDomain
                    .CurrentDomain
                    .GetAssemblies()
                    .Where(a => IgnoreSystemAndMicrosoftAssembliesFilter(a.GetName()))
                    .SelectMany(a => a.GetTypes());
        }

        /// <summary>
        /// Loads all assemblies and its references in a given path.
        /// </summary>
        /// <param name="path">The path to look for assemblies.</param>
        /// <param name="searchPattern">The search pattern.</param>
        /// <param name="assemblyFilter">The assembly filter.</param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public static void LoadAssembliesAndReferences(string path, string searchPattern = "*.dll", Func<AssemblyName, bool> assemblyFilter = null)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (searchPattern == null) throw new ArgumentNullException(nameof(searchPattern));            

            foreach (var filePath in Directory.GetFiles(path, searchPattern))
            {
                LoadAssemblyAndReferences(AssemblyName.GetAssemblyName(filePath), assemblyFilter);
            }
        }

        /// <summary>
        /// Loads an assembly and its references.
        /// Only references that are used are actually loaded, since the .NET compiler ignores assemblies that are not used in the code.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="assemblyFilter">The assembly filter.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static void LoadAssemblyAndReferences(AssemblyName assemblyName, Func<AssemblyName, bool> assemblyFilter = null)
        {
            if (assemblyName == null) throw new ArgumentNullException(nameof(assemblyName));
            var loadedAssemblieNames =
                new HashSet<string>(
                    AppDomain
                        .CurrentDomain
                        .GetAssemblies()
                        .Select(a => a.GetName().FullName));
            
            LoadAssemblyAndReferences(assemblyName, assemblyFilter, loadedAssemblieNames);
        }

        private static void LoadAssemblyAndReferences(AssemblyName assemblyName, Func<AssemblyName, bool> assemblyFilter, ISet<string> loadedAssembliesNames)
        {
            Assembly assembly;
            try
            {
                assembly = Assembly.Load(assemblyName);                
            }
            catch (Exception ex)
            {
                throw new TypeLoadException($"Could not load the assembly '{assemblyName.FullName}'", ex);
            }
            
            loadedAssembliesNames.Add(assemblyName.FullName);

            var referencedAssemblyNames =
                assembly.GetReferencedAssemblies()
                    .Where(
                        a =>
                            (assemblyFilter == null || assemblyFilter(a)) && !loadedAssembliesNames.Contains(a.FullName));

            foreach (var referencedAssemblyName in referencedAssemblyNames)
            {
                try
                {
                    LoadAssemblyAndReferences(referencedAssemblyName, assemblyFilter, loadedAssembliesNames);
                }
                catch (Exception ex)
                {
                    throw new TypeLoadException($"Could not load the referenced assembly '{referencedAssemblyName.FullName}' of assembly '{assemblyName.FullName}'", ex);
                }                
            }
        }

        private static void AddDataContractType(Type type)
        {
            _dataContractTypes.Add(type);

            if (!type.IsAbstract)
            {
                // Caches the documents (contents and resources)
                if (typeof(Document).IsAssignableFrom(type))
                {
                    var document = Activator.CreateInstance(type) as Document;
                    if (document != null)
                    {
                        _documentMediaTypeDictionary.Add(document.GetMediaType(), type);
                    }
                }

                // Caches the Authentication schemes
                if (typeof(Authentication).IsAssignableFrom(type))
                {
                    var authentication = Activator.CreateInstance(type) as Authentication;
                    if (authentication != null)
                    {
                        _authenticationSchemeDictionary.Add(authentication.GetAuthenticationScheme(), type);
                    }
                }
            }

            // Caches the enums
            if (type.IsEnum)
            {
                var enumNames = Enum.GetNames(type);
                var memberValueDictionary = new Dictionary<string, object>();

                foreach (var enumName in enumNames)
                {
                    memberValueDictionary.Add(enumName.ToLowerInvariant(), Enum.Parse(type, enumName));
                }
                _enumTypeValueDictionary.Add(type, memberValueDictionary);
            }
        }
    }
}

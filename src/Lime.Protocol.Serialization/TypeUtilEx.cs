using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Lime.Protocol.Serialization
{
    /// <summary>
    /// Provides metadata about the types.
    /// </summary>
    public static class TypeUtilEx
    {
        private static readonly ConcurrentDictionary<Type, Delegate> TypeParseDelegateDictionary = new ConcurrentDictionary<Type, Delegate>();
        private static readonly ConcurrentDictionary<Type, Func<string, object>> TypeParseFuncDictionary = new ConcurrentDictionary<Type, Func<string, object>>();

        /// <summary>
        /// Gets the Parse static method of a Type as a func.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Func<string, T> GetParseFunc<T>()
        {
            var type = typeof(T);

            Delegate parseDelegate;
            if (!TypeParseDelegateDictionary.TryGetValue(type, out parseDelegate))
            {
                var parseMethod = typeof(T)
                    .GetTypeInfo()
                    .GetMethods(BindingFlags.Static | BindingFlags.Public)
                    .FirstOrDefault(m =>
                        m.Name.Equals("Parse") &&
                        m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType == typeof(string));

                if (parseMethod == null)
                {
                    throw new ArgumentException($"The type '{type}' doesn't contains a static 'Parse(string)' method");
                }

                if (parseMethod.ReturnType != type)
                {
                    throw new ArgumentException("The Parse method has an invalid return type");
                }

                var parseFuncType = typeof(Func<,>).MakeGenericType(typeof(string), type);
                parseDelegate = parseMethod.CreateDelegate(parseFuncType);
                TypeParseDelegateDictionary.TryAdd(type, parseDelegate);
            }

            return (Func<string, T>) parseDelegate;
        }

        /// <summary>
        /// Gets the Parse static method of a Type as a func.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Func<string, object> GetParseFuncForType(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            Func<string, object> parseFunc;
            if (!TypeParseFuncDictionary.TryGetValue(type, out parseFunc))
            {
                try
                {
                    var getParseFuncMethod = typeof(TypeUtilEx)
                        .GetTypeInfo()
                        .GetMethod(nameof(GetParseFunc), BindingFlags.Static | BindingFlags.Public)
                        .MakeGenericMethod(type);

                    var genericGetParseFunc = getParseFuncMethod.Invoke(null, null);

                    var parseFuncAdapterMethod = typeof(TypeUtilEx)
                        .GetTypeInfo()
                        .GetMethod(nameof(ParseFuncAdapter), BindingFlags.Static | BindingFlags.NonPublic)
                        .MakeGenericMethod(type);

                    parseFunc = (Func<string, object>)parseFuncAdapterMethod.Invoke(null, new[] { genericGetParseFunc });
                    TypeParseDelegateDictionary.TryAdd(type, parseFunc);
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
            }

            return parseFunc; 
        }

        /// <summary>
        /// Utility function to adapt a typed Func to a object one.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parseFunc">The parse function.</param>
        /// <returns></returns>
        private static Func<string, object> ParseFuncAdapter<T>(Func<string, T> parseFunc)
        {
            return (s) => (object)parseFunc(s);
        }

        /// <summary>
        /// Try parses the string to a object of the specified type.
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
        /// Build a delegate to get a property value of a class.
        /// </summary>
        /// <a href="http://stackoverflow.com/questions/10820453/reflection-performance-create-delegate-properties-c"/>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public static Func<object, object> BuildGetAccessor(PropertyInfo propertyInfo)
        {
            return BuildGetAccessor(propertyInfo.GetGetMethod());
        }

        /// <summary>
        /// Build a delegate to get a property value of a class.
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
        /// Build a delegate to set a property value of a class.
        /// </summary>
        /// <a href="http://stackoverflow.com/questions/10820453/reflection-performance-create-delegate-properties-c"/>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public static Action<object, object> BuildSetAccessor(PropertyInfo propertyInfo)
        {
            return BuildSetAccessor(propertyInfo.GetSetMethod());
        }

        /// <summary>
        /// Build a delegate to set a property value of a class.
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


    }
}

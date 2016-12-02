using Lime.Protocol.Security;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace Lime.Protocol.Serialization
{
    /// <summary>
    /// Provides metadata information about the types.
    /// </summary>
    public static class TypeUtilEx
    {
        private static readonly ConcurrentDictionary<Type, Func<string, object>> _typeParseFuncDictionary = new ConcurrentDictionary<Type, Func<string, object>>();

        

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


    }
}

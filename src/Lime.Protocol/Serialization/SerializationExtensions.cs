using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Serialization
{
    public static class SerializationExtensions
    {
        /// <summary>
        /// Gets the default value of a Type        
        /// </summary>
        /// <a href="http://stackoverflow.com/questions/325426/programmatic-equivalent-of-defaulttype"/>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object GetDefaultValue(this Type type)
        {
            // Validate parameters.
            if (type == null) throw new ArgumentNullException(nameof(type));

            // We want an Func<object> which returns the default.
            // Create that expression here.
            var e = Expression.Lambda<Func<object>>(
                // Have to convert to object.
                Expression.Convert(
                // The default value, always get what the *code* tells us.
                    Expression.Default(type), typeof(object)
                )
            );

            // Compile and return the value.
            return e.Compile()();
        }

        public static string RemoveCrLf(this string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            return value
                .Replace("\n", string.Empty)
                .Replace("\r", string.Empty);
        }

        public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action)
        {
            foreach (var i in ie)
            {
                action(i);
            }
        }

        private const string INDENT_STRING = "  ";
        public static string IndentJson(this string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString)) throw new ArgumentNullException(nameof(jsonString));

            jsonString = jsonString.RemoveCrLf();

            var indent = 0;
            var quoted = false;
            var sb = new StringBuilder();
            for (var i = 0; i < jsonString.Length; i++)
            {
                var ch = jsonString[i];
                switch (ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, ++indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        break;
                    case '}':
                    case ']':
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, --indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        sb.Append(ch);
                        break;
                    case '"':
                        sb.Append(ch);
                        bool escaped = false;
                        var index = i;
                        while (index > 0 && jsonString[--index] == '\\')
                            escaped = !escaped;
                        if (!escaped)
                            quoted = !quoted;
                        break;
                    case ',':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        break;
                    case ':':
                        sb.Append(ch);
                        if (!quoted)
                            sb.Append(" ");
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }

        private const int LowerCaseOffset = 'a' - 'A';
        
        /// <summary>
        /// Converts the string to the
        /// camelCase representation
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToCamelCase(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            var len = value.Length;
            var newValue = new char[len];
            var firstPart = true;

            for (var i = 0; i < len; ++i)
            {
                var c0 = value[i];
                var c1 = i < len - 1 ? value[i + 1] : 'A';
                var c0isUpper = c0 >= 'A' && c0 <= 'Z';
                var c1isUpper = c1 >= 'A' && c1 <= 'Z';

                if (firstPart && c0isUpper && (c1isUpper || i == 0))
                    c0 = (char)(c0 + LowerCaseOffset);
                else
                    firstPart = false;

                newValue[i] = c0;
            }

            return new string(newValue);
        }

        /// <summary>
        /// Converts the string to the
        /// TitleCase (or PascalCase) representation
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToTitleCase(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            var len = value.Length;
            var newValue = new char[len];

            for (var i = 0; i < len; ++i)
            {
                var c0 = value[i];
                var c1 = i < len - 1 ? value[i + 1] : 'A';
                var c0isUpper = c0 >= 'A' && c0 <= 'Z';
                var c1isUpper = c1 >= 'A' && c1 <= 'Z';

                if (i == 0 && !c0isUpper)
                    c0 = (char)(c0 - LowerCaseOffset);
                else if (c0isUpper && c1isUpper)
                    c0 = (char)(c0 + LowerCaseOffset);

                newValue[i] = c0;
            }

            return new string(newValue);
        }

        /// <summary>
        /// Gets a Base64 representation of a string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToBase64(this string value)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        /// <summary>
        /// Converts from a Base64 string 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string FromBase64(this string value)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));

            var valueBytes = Convert.FromBase64String(value);

            return Encoding.UTF8.GetString(
                valueBytes,
                0,
                valueBytes.Length);
        }

        public static string Escape(this string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}

using System;
using System.Linq.Expressions;

namespace Lime.Protocol
{
    public static class TypeExtensions
    {
        public static EnvelopeType GetEnvelopeType(this Type type)
        {                    
            if (type == typeof(Notification)) return EnvelopeType.Notification; 
            if (type == typeof(Message)) return EnvelopeType.Message;
            if (type == typeof(Command)) return EnvelopeType.Command;
            if (type == typeof(Session)) return EnvelopeType.Session;
            throw new ArgumentException("Invalid envelope type");                    
        }

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
    }
}
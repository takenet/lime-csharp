using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    public static class UtilExtensions
    {
        /// <summary>
        /// Disposes an object if it is not null
        /// and it implements IDisposable interface
        /// </summary>
        /// <param name="source"></param>
        public static void DisposeIfDisposable<T>(this T source) where T : class
        {
            if (source != null &&
                source is IDisposable)
            {
                ((IDisposable)source).Dispose();
            }
        }

        /// <summary>
        /// Checks if an event handles is not null and
        /// raise it if is the case
        /// </summary>
        /// <param name="event"></param>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void RaiseEvent(this EventHandler @event, object sender, EventArgs e)
        {
            if (@event != null)
                @event(sender, e);
        }

        /// <summary>
        /// Checks if an event handles is not null and
        /// raise it if is the case
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="event"></param>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void RaiseEvent<T>(this EventHandler<T> @event, object sender, T e)
            where T : EventArgs
        {
            if (@event != null)
                @event(sender, e);
        }

        /// <summary>
        /// Allow cancellation of non-cancellable tasks        
        /// </summary>
        /// <a href="http://blogs.msdn.com/b/pfxteam/archive/2012/10/05/how-do-i-cancel-non-cancelable-async-operations.aspx"/>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(
                        s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
                if (task != await Task.WhenAny(task, tcs.Task))
                    throw new OperationCanceledException(cancellationToken);
            return await task;
        }

        /// <summary>
        /// Allow cancellation of non-cancellable tasks        
        /// </summary>
        /// <a href="http://blogs.msdn.com/b/pfxteam/archive/2012/10/05/how-do-i-cancel-non-cancelable-async-operations.aspx"/>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task WithCancellation(this Task task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(
                        s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
                if (task != await Task.WhenAny(task, tcs.Task))
                    throw new OperationCanceledException(cancellationToken);
            await task;
        }

        /// <summary>
        /// Converts a SecureString to a regular, unsecure string.        
        /// </summary>
        /// <a href="http://blogs.msdn.com/b/fpintos/archive/2009/06/12/how-to-properly-convert-securestring-to-string.aspx"/>
        /// <param name="securePassword"></param>
        /// <returns></returns>
        public static string ToUnsecureString(this SecureString securePassword)
        {
            if (securePassword == null)
            {
                throw new ArgumentNullException("securePassword");
            }

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        /// <summary>
        /// Converts a regular string to a SecureString
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static SecureString ToSecureString(this string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            var secureString = new SecureString();
            foreach (var c in value)
            {
                secureString.AppendChar(c);
            }
            return secureString;
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
            if (type == null) throw new ArgumentNullException("type");

            // We want an Func<object> which returns the default.
            // Create that expression here.
            Expression<Func<object>> e = Expression.Lambda<Func<object>>(
                // Have to convert to object.
                Expression.Convert(
                // The default value, always get what the *code* tells us.
                    Expression.Default(type), typeof(object)
                )
            );

            // Compile and return the value.
            return e.Compile()();
        }

        /// <summary>
        /// Creates a CancellationToken
        /// with the specified delay
        /// </summary>
        /// <param name="delay">The delay.</param>
        /// <returns></returns>
        public static CancellationToken ToCancellationToken(this TimeSpan delay)
        {
            var cts = new CancellationTokenSource(delay);
            return cts.Token;
        }

    }
}

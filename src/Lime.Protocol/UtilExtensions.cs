using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;

namespace Lime.Protocol
{
    public static class UtilExtensions
    {
        public const string PING_URI_TEMPLATE = "/ping";


        /// <summary>
        /// Indicates if a command is
        /// a ping request
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static bool IsPingRequest(this Command command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            if (command.Method == CommandMethod.Get &&
                command.Status == CommandStatus.Pending &&
                command.Uri != null)
            {
                if (command.Uri.IsRelative)
                {
                    return command.Uri.Path.Equals(PING_URI_TEMPLATE, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    return command.Uri.ToUri().LocalPath.Equals(PING_URI_TEMPLATE, StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the Uri address
        /// of a command resource
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static Uri GetResourceUri(this Command command)
        {
            if (command.Uri == null)
            {
                throw new ArgumentException("The command 'uri' value is null");
            }

            if (command.Uri.IsRelative)
            {
                if (command.From == null)
                {
                    throw new ArgumentException("The command 'from' value is null");
                }

                return command.Uri.ToUri(command.From);
            }
            else
            {
                return command.Uri.ToUri();
            }            
        }


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
            if (securePassword == null) throw new ArgumentNullException(nameof(securePassword));
            
            var unmanagedString = IntPtr.Zero;
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
        /// Creates a CancellationToken
        /// with the specified delay
        /// </summary>
        /// <param name="delay">The delay.</param>
        /// <returns></returns>
        [Obsolete("This method should be avoided since uses a CancellationTokenSource which needs to be disposed")]
        public static CancellationToken ToCancellationToken(this TimeSpan delay)
        {
            var cts = new CancellationTokenSource(delay);
            return cts.Token;
        }

        /// <summary>
        /// Gets the identity value from 
        /// the certificate subject
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static Identity GetIdentity(this X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException("certificate");
            }            

            var identityName = certificate.GetNameInfo(
                X509NameType.SimpleName, 
                false);

            Identity identity = null;

            if (!string.IsNullOrWhiteSpace(identityName))
            {
                Identity.TryParse(identityName, out identity);
            }

            return identity;
        }

        /// <summary>
        /// Gets the identity
        /// associated to the URI 
        /// authority
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static Identity GetIdentity(this Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (uri.HostNameType != UriHostNameType.Dns)
            {
                throw new ArgumentException("The uri hostname must be a dns value");
            }

            return new Identity()
            {
                Name = uri.UserInfo,
                Domain = uri.Host
            };
        }

        /// <summary>
        /// Transform to a flat string
        /// with comma sepparate values.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string ToCommaSepparate(this IEnumerable<string> values)
        {
            return values.Aggregate((a, b) => string.Format("{0},{1}", a, b)).TrimEnd(',');
        }

        private static Regex formatRegex = new Regex(@"({)([^}]+)(})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Format the string using
        /// the source object to populate
        /// the named formats.
        /// http://www.hanselman.com/blog/CommentView.aspx?guid=fde45b51-9d12-46fd-b877-da6172fe1791
        /// </summary>
        /// <param name="format"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string NamedFormat(this string format, object source)
        {
            return NamedFormat(format, source, null);
        }

        /// <summary>
        /// Format the string using
        /// the source object to populate
        /// the named formats.
        /// http://www.hanselman.com/blog/CommentView.aspx?guid=fde45b51-9d12-46fd-b877-da6172fe1791
        /// </summary>
        /// <param name="format"></param>
        /// <param name="source">The format names source object.</param>
        /// <param name="formatProvider">The format provider for the ToString method.</param>
        /// <returns></returns>
        public static string NamedFormat(this string format, object source, IFormatProvider formatProvider)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }

            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            StringBuilder sb = new StringBuilder();
            Type type = source.GetType();

            MatchCollection mc = formatRegex.Matches(format);
            int startIndex = 0;
            foreach (Match m in mc)
            {
                Group g = m.Groups[2]; //it's second in the match between { and }  
                int length = g.Index - startIndex - 1;
                sb.Append(format.Substring(startIndex, length));

                string toGet = String.Empty;
                string toFormat = String.Empty;
                int formatIndex = g.Value.IndexOf(":"); //formatting would be to the right of a :  
                if (formatIndex == -1) //no formatting, no worries  
                {
                    toGet = g.Value;
                }
                else //pickup the formatting  
                {
                    toGet = g.Value.Substring(0, formatIndex);
                    toFormat = g.Value.Substring(formatIndex + 1);
                }

                //first try properties  
                var retrievedProperty = type.GetProperty(toGet);
                Type retrievedType = null;
                object retrievedObject = null;
                if (retrievedProperty != null)
                {
                    retrievedType = retrievedProperty.PropertyType;
                    retrievedObject = retrievedProperty.GetValue(source, null);
                }
                else //try fields  
                {
                    var retrievedField = type.GetField(toGet);
                    if (retrievedField != null)
                    {
                        retrievedType = retrievedField.FieldType;
                        retrievedObject = retrievedField.GetValue(source);
                    }
                }

                if (retrievedType != null) //Cool, we found something  
                {
                    string result = String.Empty;
                    if (toFormat == String.Empty) //no format info  
                    {
                        result = retrievedType.InvokeMember("ToString",
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.IgnoreCase
                            , null, retrievedObject, null) as string;
                    }
                    else //format info  
                    {
                        result = retrievedType.InvokeMember("ToString",
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.IgnoreCase
                            , null, retrievedObject, new object[] { toFormat, formatProvider }) as string;
                    }
                    sb.Append(result);
                }
                else //didn't find a property with that name, so be gracious and put it back  
                {
                    sb.Append("{");
                    sb.Append(g.Value);
                    sb.Append("}");
                }
                startIndex = g.Index + g.Length + 1;
            }
            if (startIndex < format.Length) //include the rest (end) of the string  
            {
                sb.Append(format.Substring(startIndex));
            }
            return sb.ToString();
        }


        /// <summary>
        /// Gets a SHA1 hash for the specified string.
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public static byte[] ToSHA1Hash(this string inputString)
        {
            using (var algorithm = SHA1.Create())
            {
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
            }                
        }

        /// <summary>
        /// Gets a SHA1 hash for the specified string.
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public static string ToSHA1HashString(this string inputString)
        {
            var sb = new StringBuilder();
            foreach (byte b in ToSHA1Hash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        /// <summary>
        /// Creates a completed task.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static Task<T> AsCompletedTask<T>(this T value)
        {
            return Task.FromResult<T>(value);
        }

        /// <summary>
        /// Converts an exception to a general error reason.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns></returns>
        public static Reason ToReason(this Exception exception)
        {
            return new Reason()
            {
                Code = ReasonCodes.GENERAL_ERROR,
                Description = exception.Message
            };            
        }
    }
}

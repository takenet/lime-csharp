using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Security
{
    /// <summary>
    /// Defines a plain authentication scheme,
    /// that uses a password for authentication.
    /// Should be used only with encrypted sessions.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class PlainAuthentication : Authentication, IDisposable
    {
        public const string PASSWORD_KEY = "password";

        public PlainAuthentication()
            : base(AuthenticationScheme.Plain)
        {

        }

        ~PlainAuthentication()
        {
            this.Dispose(false);
        } 


#if !PCL
        [IgnoreDataMember]
        private SecureString _password;

        /// <summary>
        /// Base64 representation of the 
        /// identity password
        /// </summary>
        [DataMember(Name = PASSWORD_KEY)]
        public string Password
        {
            get
            {
                if (_password != null)
                {
                    IntPtr unmanagedString = IntPtr.Zero;
                    try
                    {
                        unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(_password);
                        return Marshal.PtrToStringUni(unmanagedString);
                    }
                    finally
                    {
                        Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
                    }
                }
                return null;
            }
            set
            {
                if (_password != null)
                {
                    _password.Dispose();
                    _password = null;
                }

                if (value != null)
                {
                    _password = new SecureString();
                    foreach (var c in value)
                    {
                        _password.AppendChar(c);
                    }
                }
            }
        } 

#else

        [IgnoreDataMember]
        private string _password;

        /// <summary>
        /// Base64 representation of the 
        /// identity password
        /// </summary>
        [DataMember(Name = PASSWORD_KEY)]
        public string Password 
        { 
            get { return _password; }
            set { _password = value; }
        }
#endif

        /// <summary>
        /// Set a plain password to a 
        /// Base64 representation
        /// </summary>
        /// <param name="password"></param>
        public void SetToBase64Password(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                this.Password = password;
            }
            else
            {
                this.Password = password.ToBase64();
            }
        }

        /// <summary>
        /// Gets the plain password decoded 
        /// from the Base64 representation
        /// </summary>
        /// <returns></returns>
        public string GetFromBase64Password()
        {
            if (string.IsNullOrWhiteSpace(this.Password))
            {
                return this.Password;
            }
            else
            {
                return this.Password.ToFrom64();
            }
        }

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
#if !PCL
                if (_password != null)
                {
                    _password.Dispose();
                } 
#endif
            }
        }

        #endregion 

    }
}

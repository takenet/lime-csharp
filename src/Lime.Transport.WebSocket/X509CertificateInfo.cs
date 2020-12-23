using System;
using System.Security.Cryptography.X509Certificates;

namespace Lime.Transport.WebSocket
{
    /// <summary>
    /// Provides information about a stored certificate.
    /// </summary>
    public sealed class X509CertificateInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="X509CertificateInfo"/> class.
        /// </summary>
        /// <param name="thumbprint">The thumbprint.</param>
        /// <param name="store">The store.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public X509CertificateInfo(string thumbprint, StoreName store = StoreName.My)
        {
            if (thumbprint == null) throw new ArgumentNullException(nameof(thumbprint));
            Thumbprint = thumbprint;
            Store = store;
        }

        /// <summary>
        /// Gets the certificate thumbprint.
        /// </summary>
        /// <value>
        /// The thumbprint.
        /// </value>
        public string Thumbprint { get; }

        /// <summary>
        /// Gets the certificate store.
        /// </summary>
        /// <value>
        /// The store.
        /// </value>
        public StoreName Store { get; }
    }
}
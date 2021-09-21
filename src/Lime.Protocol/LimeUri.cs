using System;
using System.Net;

namespace Lime.Protocol
{
    /// <summary>
    /// Represents an URI of the lime scheme.
    /// </summary>
    public sealed class LimeUri
    {
        public const string LIME_URI_SCHEME = "lime";

        private readonly Uri _absoluteUri;

        /// <summary>
        /// Initializes a new instance of the <see cref="LimeUri"/> class.
        /// </summary>
        /// <param name="uriPath">The URI path.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentException">
        /// Invalid URI format
        /// </exception>
        public LimeUri(string uriPath)
        {
            if (string.IsNullOrWhiteSpace(uriPath)) throw new ArgumentNullException(nameof(uriPath));

            if (Uri.IsWellFormedUriString(uriPath, UriKind.Absolute))
            {
                _absoluteUri = new Uri(uriPath);
                ValidatLimeScheme(_absoluteUri);
            }
            else if (!Uri.IsWellFormedUriString(uriPath, UriKind.Relative))
            {
                // TODO: This 'if' statement is only necessary while the related issue is not fixed
                // Issue: https://github.com/dotnet/runtime/issues/21626
                if (Uri.TryCreate(uriPath, UriKind.Absolute, out var receivedUri) &&
                    ReceivedUriPathIsEncoded(uriPath) &&
                    Uri.IsWellFormedUriString($"{receivedUri.Scheme}://{receivedUri.UserInfo}@{receivedUri.Host}", UriKind.Absolute) &&
                    Uri.IsWellFormedUriString(receivedUri.PathAndQuery + receivedUri.Fragment, UriKind.Relative))
                {
                    _absoluteUri = new Uri(uriPath);
                    ValidatLimeScheme(_absoluteUri);
                }
                else
                {
                    throw new ArgumentException("Invalid URI format");
                }
            }
            Path = uriPath.TrimEnd('/');
        }

        /// <summary>
        /// Fragment or complete
        /// URI path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Indicates if the path 
        /// is relative.
        /// </summary>
        public bool IsRelative => _absoluteUri == null;

        /// <summary>
        /// Convert the current
        /// absolute path to a Uri.
        /// </summary>
        /// <returns></returns>
        public Uri ToUri()
        {
            if (_absoluteUri == null)
            {
                throw new InvalidOperationException("The URI path is relative");
            }

            return _absoluteUri;
        }

        /// <summary>
        /// Convert the relative
        /// path to a Uri, using
        /// the identity as the
        /// URI authority.
        /// </summary>
        /// <param name="authority"></param>
        /// <returns></returns>
        public Uri ToUri(Identity authority)
        {
            if (_absoluteUri != null)
            {
                throw new InvalidOperationException("The URI path is absolute");
            }

            if (authority == null)
            {
                throw new ArgumentNullException(nameof(authority));
            }

            var baseUri = GetBaseUri(authority);
            return new Uri(baseUri, Path);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return ToString().ToLowerInvariant().GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            var limeUri = obj as LimeUri;
            return limeUri != null && Path.Equals(limeUri.Path, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() => Path;

        /// <summary>
        /// Parses the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static LimeUri Parse(string value)
        {
            return new LimeUri(value);
        }

        public static Uri GetBaseUri(Identity authority)
        {
            return new Uri($"{LIME_URI_SCHEME}://{authority}/");
        }
        
        public static implicit operator LimeUri(string value) => value == null ? null : Parse(value);
        
        public static implicit operator string(LimeUri limeUri) => limeUri?.ToString();

        // TODO: Remove this method once the 'if' statement on the ctor is removed
        private bool ReceivedUriPathIsEncoded(string uri)
            => WebUtility.UrlDecode(uri) != uri;

        private void ValidatLimeScheme(Uri absoluteUri)
        {
            if (!absoluteUri.Scheme.Equals(LIME_URI_SCHEME))
            {
                throw new ArgumentException($"Invalid URI scheme. Expected is '{LIME_URI_SCHEME}'");
            }
        }
    }
}

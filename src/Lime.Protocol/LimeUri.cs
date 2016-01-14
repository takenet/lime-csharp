using System;
using System.Collections.Generic;
using System.Text;

namespace Lime.Protocol
{
    /// <summary>
    /// Represents a URI
    /// from the lime scheme.
    /// </summary>
    public sealed class LimeUri
    {
        private Uri _absoluteUri;
        public const string LIME_URI_SCHEME = "lime";

        #region Constructor

        public LimeUri(string uriPath)
        {
            if (string.IsNullOrWhiteSpace(uriPath)) throw new ArgumentNullException(nameof(uriPath));

			if (Uri.IsWellFormedUriString(uriPath, UriKind.Absolute))
            {
                _absoluteUri = new Uri(uriPath);

				#if MONO
				// In Linux, a path like '/presence' is considered
				// a valid absolute file uri

				if (_absoluteUri.Scheme.Equals(Uri.UriSchemeFile))
				{
					_absoluteUri = null;
				}
				else

				#endif

                if (!_absoluteUri.Scheme.Equals(LIME_URI_SCHEME))
                {
                    throw new ArgumentException($"Invalid URI scheme. Expected is '{LIME_URI_SCHEME}'");
                }
            }
            else if (!Uri.IsWellFormedUriString(uriPath, UriKind.Relative))
            {
                throw new ArgumentException("Invalid URI format");
            }

            this.Path = uriPath.TrimEnd('/');            
        }

        #endregion
       
        /// <summary>
        /// Fragment or complete
        /// URI path.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Indicates if the path 
        /// is relative.
        /// </summary>
        public bool IsRelative
        {
            get { return _absoluteUri == null; }
        }

        #region Public Methods

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
                throw new ArgumentNullException("authority");
            }

            var baseUri = GetBaseUri(authority);
            return new Uri(baseUri, Path);
        }

        public override int GetHashCode()
        {
            return this.ToString().ToLowerInvariant().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var limeUri = obj as LimeUri;
            if (limeUri == null)
            {
                return false;
            }

            return this.Path.Equals(limeUri.Path, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return this.Path;
        } 

        public static LimeUri Parse(string value)
        {
            return new LimeUri(value);
        }

        public static Uri GetBaseUri(Identity authority)
        {
            return new Uri(string.Format("{0}://{1}/", LIME_URI_SCHEME, authority));
        }

        #endregion
    }
}

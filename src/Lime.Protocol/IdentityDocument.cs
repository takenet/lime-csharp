using System;
using System.Collections.Generic;
using System.Text;

namespace Lime.Protocol
{
    /// <summary>
    /// Represents an identity document.
    /// </summary>
    public sealed class IdentityDocument : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.identity";
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        #region Constructor

        public IdentityDocument(string identity)
            : base(MediaType)
        {
            if (!string.IsNullOrWhiteSpace(identity))
            {
                throw new ArgumentException("Invalid identity.");
            }

            this.Value = Identity.Parse(identity);
        }

        #endregion

        /// <summary>
        /// The value of the document
        /// </summary>
        public Identity Value { get; private set; }

        public override string ToString()
        {
            return this.Value.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network.Modules;
using Lime.Protocol.Serialization;

namespace Lime.Protocol.Security
{
    public class AesEncryptionMessageModule : ChannelModuleBase<Message>
    {
        private readonly Aes _aes;

        public AesEncryptionMessageModule(IDocumentSerializer documentSerializer)
        {
            _aes = Aes.Create();
        }

        public override Task<Message> OnSendingAsync(Message envelope, CancellationToken cancellationToken)
        {
            _aes.GenerateKey();
            
            
            

            return base.OnSendingAsync(envelope, cancellationToken);
        }
    }

    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class EncryptedContainer : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.encrypted+json";
        public const string TYPE_KEY = "type";
        public const string VALUE_KEY = "value";

        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptedContainer"/> class.
        /// </summary>
        public EncryptedContainer() :
            base(MediaType)
        {
        }

        /// <summary>
        /// Gets the media type of the encrypted document.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [DataMember(Name = TYPE_KEY)]
        public MediaType Type { get; set; }

        /// <summary>
        /// Gets or sets the encrypted, base64 document value.
        /// </summary>
        /// <value>
        /// The document.
        /// </value>
        [DataMember(Name = VALUE_KEY)]
        public string Value { get; set; }
    }
}

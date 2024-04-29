using Lime.Protocol;
using System.Linq;
using System.Runtime.Serialization;

namespace Lime.Messaging.Contents
{
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Reaction : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.reaction+json";
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        public const string EMOJI_KEY = "emoji";
        public const string IN_REACTION_TO = "inReactionTo";

        /// <summary>
        /// Initializes a new instance of the <see cref="Reaction"/> class.
        /// </summary>
        public Reaction()
            : base(MediaType)
        {
        }

        /// <summary>
        /// Gets or sets the emojis associated with the reaction.
        /// </summary>
        [DataMember(Name = EMOJI_KEY)]
        public UnicodeSequence Emoji { get; set; }


        /// <summary>
        /// Gets or sets the reference to the message to which the reaction is associated.
        /// </summary>
        [DataMember(Name = IN_REACTION_TO)]
        public InReactionTo InReactionTo { get; set; }
    }

    /// <summary>
    /// Represents the document container for the reaction reference.
    /// </summary>
    [DataContract]
    public class InReactionTo
    {
        public const string ID = "id";
        public const string TYPE_KEY = "type";
        public const string VALUE_KEY = "value";
        public const string DIRECTION_KEY = "direction";

        /// <summary>
        /// Gets or sets the identifier of the message being reacted to.
        /// </summary>  
        [DataMember(Name = ID)]
        public string Id { get; set; }

        /// <summary>
        /// Gets the media type of the sensitive document.
        /// </summary>
        [DataMember(Name = TYPE_KEY)]
        public MediaType Type => Value?.GetMediaType();

        /// <summary>
        /// Gets or sets the contained document value.
        /// </summary>
        [DataMember(Name = VALUE_KEY)]
        public Document Value { get; set; }

        /// <summary>
        /// Indicates the direction of the message in the thread.
        /// </summary>
        [DataMember(Name = DIRECTION_KEY)]
        public MessageDirection? Direction { get; set; }
    }

    /// <summary>
    /// Represents a sequence of Unicode characters.
    /// </summary>
    [DataContract]
    public class UnicodeSequence
    {
        public const string VALUE_KEY = "values";

        /// <summary>
        /// Gets or sets the Unicode representation of the sequence.
        /// </summary>
        [DataMember(Name = VALUE_KEY)]
        public uint[] Values { get; set; }

        /// <summary>
        /// Converts the Unicode sequence to a string.
        /// </summary>
        /// <returns>
        /// A string representation of the Unicode sequence, or an empty string if the sequence is empty.
        /// </returns>
        public override string ToString()
        {
            return Values != null ? string.Concat(Values.Select(codePoint => char.ConvertFromUtf32((int)codePoint))) : string.Empty;
        }
    }
}

using Lime.Protocol;
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
        public Emojis Emoji { get; set; }


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
    }

    /// <summary>
    /// The current chat state, represented by emojis. For more information about available emojis, 
    /// visit the <see cref="https://design.blip.ai/d/UbKsV1JhXTK4/componentes-desenvolvimento#/icon/icones-de-emojis">documentation</see>.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public enum Emojis
    {
    
        [EnumMember(Value = "beaming-face")]
        BeamingFace,

        [EnumMember(Value = "confounded-face")]
        ConfoundedFace,
 
        [EnumMember(Value = "crying-face")]
        CryingFace,

        [EnumMember(Value = "dizzy-face")]
        DizzyFace,
 
        [EnumMember(Value = "expressionless-face")]
        ExpressionlessFace,

        [EnumMember(Value = "face-blowing-a-kiss")]
        FaceBlowingKiss,

        [EnumMember(Value = "face-with-mask")]
        FaceWithMask,

        [EnumMember(Value = "face-with-open-mouth")]
        FaceWithOpenMouth,

        [EnumMember(Value = "face-with-tears-of-joy")]
        FaceWithTearsOfJoy,

        [EnumMember(Value = "face-with-tongue")]
        FaceWithTongue,

        [EnumMember(Value = "face-without-mouth")]
        FaceWithoutMouth,

        [EnumMember(Value = "fearful-face")]
        FearfulFace,

        [EnumMember(Value = "grinning-face")]
        GrinningFace,

        [EnumMember(Value = "grinning-face-with-big-eyes")]
        GrinningFaceWithBigEyes,

        [EnumMember(Value = "grinning-face-with-smilling-eyes")]
        GrinningFaceWithSmillingEyes,

        [EnumMember(Value = "grinning-face-with-sweat")]
        GrinningFaceWithSweat,

        [EnumMember(Value = "hushed-face")]
        HushedFace,

        [EnumMember(Value = "kissing-face-with-smilling-eyes")]
        KissingFaceWithSmillingEyes,

        [EnumMember(Value = "loudly-cring-face")]
        LoudlyCringFace,

        [EnumMember(Value = "nerd-face")]
        NerdFace,

        [EnumMember(Value = "neutral-face")]
        NeutralFace,

        [EnumMember(Value = "perservering-face")]
        PerserveringFace,

        [EnumMember(Value = "pouting-face")]
        PoutingFace,

        [EnumMember(Value = "relieved-face")]
        RelievedFace,

        [EnumMember(Value = "sleeping-face")]
        SleepingFace,

        [EnumMember(Value = "slightly-frowning-face")]
        SlightlyFrowningFace,

        [EnumMember(Value = "slightly-smiling-face")]
        SlightlySmilingFace,

        [EnumMember(Value = "smiling-face")]
        SmilingFace,

        [EnumMember(Value = "smiling-face-with-halo")]
        SmilingFaceWithHalo,

        [EnumMember(Value = "smiling-face-with-heart-eyes")]
        SmilingFaceWithHeartEyes,

        [EnumMember(Value = "smiling-face-with-smiling-eyes")]
        SmilingFaceWithSmilingEyes,

        [EnumMember(Value = "smiling-face-with-sunglasses")]
        SmilingFaceWithSunglasses,

        [EnumMember(Value = "smirking-face")]
        SmirkingFace,

        [EnumMember(Value = "squirting-face-with-tongue")]
        SquirtingFaceWithTongue,

        [EnumMember(Value = "winking-face")]
        WinkingFace,

        [EnumMember(Value = "winking-face-with-tongue")]
        WinkingFaceWithTongue,
    }
}

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
        public const string MESSAGE_ID_KEY = "id";

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
        /// Gets or sets the identifier of the message to which the reaction is related.
        /// </summary>
        [DataMember(Name = MESSAGE_ID_KEY)]
        public string MessageId { get; set; }
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

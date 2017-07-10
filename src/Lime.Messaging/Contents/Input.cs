using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Represents an input request to an user.
    /// </summary>
    /// <seealso cref="Document" />
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Input : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.input+json";
        public const string LABEL_KEY = "label";
        public const string VALIDATION_KEY = "validation";

        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="Input"/> class.
        /// </summary>
        public Input()
            : base(MediaType)
        {

        }

        /// <summary>
        /// Gets or sets the input label that should be shown to the user.
        /// </summary>
        [DataMember(Name = LABEL_KEY)]
        public DocumentContainer Label { get; set; }

        /// <summary>
        /// Gets or sets the validation rules to be enforced into the user response message for the input.
        /// </summary>
        [DataMember(Name = VALIDATION_KEY)]
        public InputValidation Validation { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Label?.ToString() ?? "";
        }
    }

    /// <summary>
    /// Provide validation rules for inputs.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class InputValidation
    {
        public const string RULE_KEY = "rule";
        public const string REGEX_KEY = "regex";
        public const string TYPE_KEY = "type";
        public const string ERROR_KEY = "error";

        /// <summary>
        /// Gets or sets the validation rule to be used.
        /// </summary>
        [DataMember(Name = RULE_KEY, IsRequired = true)]
        public InputValidationRule Rule { get; set; }

        /// <summary>
        /// Gets or sets the regular expression to be used in case of the <see cref="Rule"/> value is <see cref="InputValidationRule.Regex"/>.
        /// </summary>
        [DataMember(Name = REGEX_KEY)]
        public string Regex { get; set; }

        /// <summary>
        /// Gets or sets the type to be used in case of the <see cref="Rule"/> value is <see cref="InputValidationRule.Type"/>.
        /// </summary>
        [DataMember(Name = TYPE_KEY)]
        public MediaType Type { get; set; }

        /// <summary>
        /// Gets or sets the error message text to be returned to the user in case of the input value is not valid accordingly to the defined rule.
        /// </summary>
        [DataMember(Name = ERROR_KEY)]
        public string Error { get; set; }
    }

    /// <summary>
    /// Defines the input validation rules to be applied to the user input response message.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public enum InputValidationRule
    {
        /// <summary>
        /// The value should be a text.
        /// In this case, the type of the message should be text/plain.
        /// </summary>
        [EnumMember(Value = "text")]
        Text,
        /// <summary>
        /// The value should be a number (integer or floating point).
        /// In this case, the type of the message should be text/plain.
        /// </summary>
        [EnumMember(Value = "number")]
        Number,
        /// <summary>
        /// The value should be a date (optionally with time values).
        /// In this case, the type of the message should be text/plain.
        /// </summary>
        [EnumMember(Value = "date")]
        Date,
        /// <summary>
        /// The value should be validated with a regular expression.
        /// In this case, the type of the message should be text/plain.
        /// </summary>
        [EnumMember(Value = "regex")]
        Regex,
        /// <summary>
        /// The value should be of a specific media type.
        /// </summary>
        [EnumMember(Value = "type")]
        Type,
    }
}
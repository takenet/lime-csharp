using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Lime.Protocol;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Represents the status of the payment of an invoice.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Document" />
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class InvoiceStatus : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.invoice-status+json";
        public const string STATUS_KEY = "status";
        public const string DATE_KEY = "date";
        public const string CODE_KEY = "code";

        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="InvoiceStatus"/> class.
        /// </summary>
        public InvoiceStatus()
            : base(MediaType)
        {
        }

        /// <summary>
        /// Gets or sets the current invoice status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        [DataMember(Name = STATUS_KEY)]
        public InvoiceStatusStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the status date.
        /// </summary>
        /// <value>
        /// The date.
        /// </value>
        [DataMember(Name = DATE_KEY)]
        public DateTimeOffset? Date { get; set; }

        /// <summary>
        /// Gets or sets the status transaction code.
        /// </summary>
        /// <value>
        /// The code.
        /// </value>
        [DataMember(Name = CODE_KEY)]
        public string Code { get; set; }
    }

    /// <summary>
    /// Defines the possible invoice payment status values.
    /// </summary>
    [DataContract]
    public enum InvoiceStatusStatus
    {
        /// <summary>
        /// Indicates that the payment operation was complete.
        /// </summary>
        [EnumMember(Value = "completed")]
        Completed,
        /// <summary>
        /// Indicates that the payment operation was cancelled by any of the parties.
        /// </summary>
        [EnumMember(Value = "cancelled")]
        Cancelled,
        /// <summary>
        /// Indicates that a previously completed payment operation was refunded to the payer.
        /// </summary>
        [EnumMember(Value = "refunded")]
        Refunded
    }
}

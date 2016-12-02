using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.UnitTests.Serialization.Models
{
    [DataContract]
    public class Schedule : Document
    {
        public const string MIME_TYPE = "application/vnd.iris.schedule+json";
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        public const string WHEN = "when";
        public const string MESSAGE = "message";

        public Schedule()
            : base(MediaType)
        { }

        [DataMember(Name = WHEN)]
        public DateTimeOffset When { get; set; }
        
        [DataMember(Name = MESSAGE)]
        public Message Message { get; set; }
    }
}

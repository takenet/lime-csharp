using System.Runtime.Serialization;

namespace Lime.Protocol.UnitTests.Serialization
{
    [DataContract]
    public class TestDocument : Document
    {
        public const string MIME_TYPE = "application/vnd.takenet.testdocument+json";

        public TestDocument()
            : base(MediaType.Parse(MIME_TYPE))
        { }

        [DataMember(Name="double")]
        public double Double { get; set; }
        [DataMember]
        public double? NullableDouble { get; set; }
    }
}

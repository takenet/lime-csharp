using Lime.Protocol.Serialization;
using NUnit.Framework;
using Shouldly;
using System.Runtime.Serialization;

namespace Lime.Protocol.UnitTests.Serialization
{
    [TestFixture]
    public class JsonSerializerTests
    {
        [Test]
        public void Deserialize_RandomObject_ReturnsValidInstance()
        {
            // Arrange
            var json = "{\"Double\":10.1, \"NullableDouble\": 10.2}";

            // Act
            var document = JsonSerializer<TestDocument>.Deserialize(json);

            // Assert
            document.Double.ShouldBe(10.1d);
            document.NullableDouble.ShouldBe(10.2d);
        }

        [Test]
        public void Deserialize_RandomObjectWithNullable_ReturnsValidInstance()
        {
            // Arrange
            var json = "{\"Double\":10.1, \"NullableDouble\": null}";

            // Act
            var document = JsonSerializer<TestDocument>.Deserialize(json);

            // Assert
            document.Double.ShouldBe(10.1d);
            document.NullableDouble.ShouldBe(null);
        }

        [DataContract]
        public class TestDocument : Document
        {
            public TestDocument()
                : base(MediaType.Parse("application/vnd.takenet.testdocument+json"))
            { }

            [DataMember]
            public double Double { get; set; }
            [DataMember]
            public double? NullableDouble { get; set; }
        }

    }
}

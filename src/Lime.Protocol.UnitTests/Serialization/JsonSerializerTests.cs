using Lime.Protocol.Serialization;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests.Serialization
{
    [TestFixture]
    public class JsonSerializerTests
    {
        [Test]
        public void Deserialize_RandomObject_ReturnsValidInstance()
        {
            // Arrange
            var json = "{\"double\":10.1, \"NullableDouble\": 10.2}";

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
            var json = "{\"double\":10.1, \"NullableDouble\": null}";

            // Act
            var document = JsonSerializer<TestDocument>.Deserialize(json);

            // Assert
            document.Double.ShouldBe(10.1d);
            document.NullableDouble.ShouldBe(null);
        }
    }
}

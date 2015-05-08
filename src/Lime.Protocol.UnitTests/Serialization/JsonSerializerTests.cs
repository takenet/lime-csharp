using Lime.Protocol.Serialization;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests.Serialization
{
    [TestFixture]
    public class JsonSerializerTests
    {
        [Test]
        public void Serialize_RandomObject_ReturnsValidJson()
        {
            // Arrange
            var nodeAddresss = "andre@takenet.com.br";
            var document = new TestDocument
            {
                Double = 1.12,
                Address = Node.Parse(nodeAddresss)
            };

            // Act
            var json = JsonSerializer<TestDocument>.Serialize(document);

            // Assert
            Assert.IsTrue(json.HasValidJsonStackedBrackets());
            Assert.IsTrue(json.ContainsJsonProperty("Address", nodeAddresss));
            Assert.IsTrue(json.ContainsJsonProperty("double", 1.12d));
        }

        [Test]
        public void Serialize_CollectionOfRandomObject_ReturnsValidJson()
        {
            // Arrange
            var nodeAddresss = "andre@takenet.com.br";
            var document = new TestDocument
            {
                Double = 1.12,
                Address = Node.Parse(nodeAddresss)
            };

            var collection = new DocumentCollection
            {
                Items = new[] { document },
                ItemType = document.GetMediaType(),
                Total = 1
            };

            // Act
            var json = JsonSerializer<DocumentCollection>.Serialize(collection);

            // Assert
            Assert.IsTrue(json.HasValidJsonStackedBrackets());
            Assert.IsTrue(json.ContainsJsonProperty("Address", nodeAddresss));
            Assert.IsTrue(json.ContainsJsonProperty("double", 1.12d));
        }

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

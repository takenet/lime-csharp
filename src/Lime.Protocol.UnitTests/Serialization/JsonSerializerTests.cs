using Lime.Protocol.Serialization;
using Xunit;
using Shouldly;
using System;

namespace Lime.Protocol.UnitTests.Serialization
{
    
    public class JsonSerializerTests
    {
        [Fact]
        public void Serialize_RandomObject_ReturnsValidJson()
        {
            // Arrange
            var nodeAddresss = "andre@takenet.com.br";
            var date = DateTime.UtcNow;
            var document = new TestDocument
            {
                Double = 1.12,
                Address = Node.Parse(nodeAddresss),
                Date = date
            };

            // Act
            var json = JsonSerializer<TestDocument>.Serialize(document);

            // Assert
            json.HasValidJsonStackedBrackets().ShouldBe(true);
            json.ContainsJsonProperty("Address", nodeAddresss).ShouldBe(true);
            json.ContainsJsonProperty("double", 1.12d).ShouldBe(true);
            json.ContainsJsonProperty("date", date).ShouldBe(true);
        }

        [Fact]
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
            json.HasValidJsonStackedBrackets().ShouldBe(true);
            json.ContainsJsonProperty("Address", nodeAddresss).ShouldBe(true);
            json.ContainsJsonProperty("double", 1.12d).ShouldBe(true);
        }

        [Fact]
        public void Deserialize_RandomObject_ReturnsValidInstance()
        {
            // Arrange
            var dateString = "2015-10-01T13:20:45.123456Z";
            var json = string.Format("{{\"double\":10.1, \"NullableDouble\": 10.2, \"date\": \"{0}\"}}", dateString);

            // Act
            var document = JsonSerializer<TestDocument>.Deserialize(json);

            // Assert
            document.Double.ShouldBe(10.1d);
            document.NullableDouble.ShouldBe(10.2d);
            document.Date.ShouldBe(DateTime.Parse(dateString));
        }

        [Fact]
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

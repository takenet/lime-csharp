using Lime.Protocol.Serialization;
using NUnit.Framework;
using Shouldly;
using System;
using Lime.Protocol.UnitTests.Serialization.Models;
using Lime.Messaging.Contents;
using Lime.Protocol.Serialization.Newtonsoft;

namespace Lime.Protocol.UnitTests.Serialization
{
    [TestFixture]
    public class JsonSerializerTests
    {
        [Test]
        public void Serialize_Deserialize_DocumentWithEnvelope()
        {
            var schedule = new Schedule();
            schedule.When = DateTimeOffset.Now.AddDays(1);
            schedule.Message = new Message()
            {
                Id = Guid.NewGuid(),
                Content = new PlainText() { Text = "Teste 5" },
                From = new Node("limeUser", "limeprotocol.org", null),
                To = new Node("limeUser", "limeprotocol.org", null)
            };

            var command = new Command()
            {
                Resource = schedule,
                Id = Guid.NewGuid(),
                From = new Node("limeUser", "limeprotocol.org", null),
                To = new Node("limeUser", "limeprotocol.org", null),
                Method = CommandMethod.Set,
                Uri = new LimeUri("/scheduler")
            };
            
            var serializer = new JsonNetSerializer();
            var json = serializer.Serialize(command);
            var deserializedCommand = (Command)serializer.Deserialize(json);
        }

        [Test]
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
            json.HasValidJsonStackedBrackets().ShouldBe(true);
            json.ContainsJsonProperty("Address", nodeAddresss).ShouldBe(true);
            json.ContainsJsonProperty("double", 1.12d).ShouldBe(true);
        }

        [Test]
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

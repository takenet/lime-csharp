using System;
using Lime.Messaging.Contents;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests
{
    [TestFixture]
    public class DocumentExtensionsTests
    {
        [Test]
        [Category("ToDocumentContainer")]
        public void Select_ToContainer()
        {
            // Arrange
            var select = new Select();

            // Act
            var container = select.ToDocumentContainer();

            // Assert
            container.Value.ShouldBe(select);
            container.Type.ShouldBe(Select.MediaType);
        }

        [Test]
        [Category("ToDocumentContainer")]
        public void Collection_ToContainer()
        {
            // Arrange
            var collection = new DocumentCollection
            {
                ItemType = PlainText.MIME_TYPE,
                Items = new PlainText[]
                {
                    new PlainText
                    {
                        Text = "Text 1"
                    },
                    new PlainText
                    {
                        Text = "Text 2"
                    },
                    new PlainText
                    {
                        Text = "Text 3"
                    }
                }
            };
            // Act
            var container = collection.ToDocumentContainer();

            // Assert
            container.Value.ShouldBe(collection);
        }

        [Test]
        [Category("ToDocumentContainer")]
        public void DocumentSelect_ToContainer()
        {
            // Arrange
            var documentSelect = new DocumentSelect
            {
                Header = PlainText.Parse("Unit tests").ToDocumentContainer(),
                Options = new DocumentSelectOption[]
                {
                    new DocumentSelectOption
                    {
                        Label = PlainText.Parse("Unit test").ToDocumentContainer(),
                        Value = PlainText.Parse("Unit test").ToDocumentContainer()
                    }
                },
                Scope = SelectScope.Immediate
            };

            // Act
            var container = documentSelect.ToDocumentContainer();

            // Assert
            container.Value.ShouldBe(documentSelect);
            container.Type.ShouldBe(DocumentSelect.MediaType);
        }

        [Test]
        [Category("ToDocumentContainer")]
        public void PlainText_ToContainer()
        {
            // Arrange
            var plainText = PlainText.Parse("Unit tests");

            // Act
            var container = plainText.ToDocumentContainer();

            // Assert
            container.Value.ShouldBe(plainText);
            container.Type.ShouldBe(PlainText.MediaType);
        }

        [Test]
        [Category("ToDocumentContainer")]
        public void MediaLink_ToContainer()
        {
            // Arrange
            var imageUri = new Uri("http://2.bp.blogspot.com/-pATX0YgNSFs/VP-82AQKcuI/AAAAAAAALSU/Vet9e7Qsjjw/s1600/Cat-hd-wallpapers.jpg", UriKind.Absolute);
            var previewUri = new Uri("https://encrypted-tbn3.gstatic.com/images?q=tbn:ANd9GcS8qkelB28RstsNxLi7gbrwCLsBVmobPjb5IrwKJSuqSnGX4IzX", UriKind.Absolute);

            var mediaLink = new MediaLink
            {
                Title = "Cat",
                Text = "Here is a cat image for you!",
                Type = MediaType.Parse("image/jpeg"),
                AspectRatio = "1:1",
                Size = 227791,
                Uri = imageUri,
                PreviewUri = previewUri
            };
            
            // Act
            var container = mediaLink.ToDocumentContainer();

            // Assert
            container.Value.ShouldBe(mediaLink);
            container.Type.ShouldBe(MediaLink.MediaType);
        }
    }
}

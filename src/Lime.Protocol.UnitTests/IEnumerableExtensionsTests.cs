using System;
using System.Collections.Generic;
using System.Linq;
using Lime.Messaging.Contents;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests
{
    [TestFixture]
    [Category("ToDocumentCollection")]
    public class IEnumerableExtensionsTests
    {
        [Test]
        public void List_ToDocumentCollection_ReturnsDocumentCollection()
        {
            // Arrange
            var documentList = new List<Document>
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
            };

            // Act
            var collection = documentList.ToDocumentCollection();

            // Assert
            collection.Total.ShouldBe(documentList.Count);
            collection.ItemType.ShouldBe(documentList.FirstOrDefault().GetMediaType());
            collection.Items.ShouldBe(documentList);
        }

        [Test]
        public void Array_ToDocumentCollection_ReturnsDocumentCollection()
        {
            // Arrange
            var documentArray = new Document[]
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
            };

            // Act
            var collection = documentArray.ToDocumentCollection();

            // Assert
            collection.Total.ShouldBe(documentArray.Count());
            collection.ItemType.ShouldBe(documentArray.FirstOrDefault().GetMediaType());
            collection.Items.ShouldBe(documentArray);
        }
    }
}

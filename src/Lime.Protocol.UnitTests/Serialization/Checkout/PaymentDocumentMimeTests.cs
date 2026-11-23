using System;
using System.Linq;
using Lime.Messaging.Resources.Checkout;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests.Serialization.Checkout
{
    [TestFixture]
    public class PaymentDocumentMimeTests
    {
        private static string[] GetMessagingMimeTypes() =>
            PaymentDocumentCatalog
                .GetConcreteDocumentTypes(PaymentDocumentCatalog.MessagingAssembly)
                .Select(PaymentDocumentCatalog.GetMimeType)
                .ToArray();

        [Test]
        public void MessagingAssembly_EveryDocument_HasUniqueMimeType()
        {
            var mimeTypes = GetMessagingMimeTypes();

            var duplicates = PaymentDocumentCatalog.FindDuplicates(mimeTypes);

            mimeTypes.Length.ShouldBeGreaterThan(PaymentDocumentCatalog.DocumentMimeTypes.Count);
            duplicates.ShouldBeEmpty();
        }

        [Test]
        public void MessagingAssembly_EveryDocument_HasLowerCaseMimeType()
        {
            var mimeTypes = GetMessagingMimeTypes();

            var upperCase = PaymentDocumentCatalog.FindWithUpperCase(mimeTypes);

            upperCase.ShouldBeEmpty();
        }

        [Test]
        public void MessagingAssembly_ContainsEveryPaymentDocument()
        {
            var documentTypes = PaymentDocumentCatalog.GetConcreteDocumentTypes(PaymentDocumentCatalog.MessagingAssembly);

            foreach (var documentType in PaymentDocumentCatalog.DocumentTypes)
            {
                documentTypes.ShouldContain(documentType);
            }
        }

        [Test]
        public void FindDuplicates_SameMimeTypeTwice_ReturnsIt()
        {
            var mimeTypes = new[]
            {
                "application/vnd.example.a+json",
                "application/vnd.example.b+json",
                "application/vnd.example.A+json",
            };

            var duplicates = PaymentDocumentCatalog.FindDuplicates(mimeTypes);

            duplicates.ShouldBe(new[] { "application/vnd.example.a+json" });
        }

        [Test]
        public void FindWithUpperCase_MimeTypeWithUpperCase_ReturnsIt()
        {
            var mimeTypes = new[] { "application/vnd.example.a+json", "application/vnd.example.B+json" };

            var upperCase = PaymentDocumentCatalog.FindWithUpperCase(mimeTypes);

            upperCase.ShouldBe(new[] { "application/vnd.example.B+json" });
        }

        [Test]
        public void PaymentCheckoutRequest_MimeType_DiffersFromCheckoutDocument()
        {
            PaymentCheckoutRequest.MIME_TYPE.ShouldNotBe(CheckoutDocument.MIME_TYPE);
            PaymentCheckoutRequest.MediaType.ShouldNotBe(CheckoutDocument.MediaType);
        }

        [TestCaseSource(nameof(DocumentCases))]
        public void PaymentDocument_MimeType_MatchesContract(Type documentType, string expectedMimeType)
        {
            var constant = (string)documentType.GetField("MIME_TYPE").GetValue(null);
            var mediaType = (MediaType)documentType.GetField("MediaType").GetValue(null);

            constant.ShouldBe(expectedMimeType);
            mediaType.ToString().ShouldBe(expectedMimeType);
            PaymentDocumentCatalog.GetMimeType(documentType).ShouldBe(expectedMimeType);
        }

        private static object[] DocumentCases() =>
            PaymentDocumentCatalog.DocumentMimeTypes
                .Select(p => new object[] { p.Key, p.Value })
                .ToArray<object>();
    }
}

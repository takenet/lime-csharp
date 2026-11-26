using System;
using System.Linq;
using Lime.Messaging;
using Lime.Messaging.Resources.Checkout;
using Lime.Protocol.Serialization;
using NUnit.Framework;
using Shouldly;

#pragma warning disable CS0618 // The obsolete document is checked against the new documents.

namespace Lime.Protocol.UnitTests.Serialization.Checkout
{
    [TestFixture]
    public class PaymentDocumentRegistrationTests
    {
        private static IDocumentTypeResolver GetTarget() => new DocumentTypeResolver().WithMessagingDocuments();

        [Test]
        public void WithMessagingDocuments_WholeAssembly_RegistersWithoutException()
        {
            IDocumentTypeResolver resolver = null;

            Should.NotThrow(() => resolver = GetTarget());

            resolver.ShouldNotBeNull();
        }

        [Test]
        public void MessagingAssembly_EveryConcreteDocument_HasPublicParameterlessConstructor()
        {
            var documentTypes = PaymentDocumentCatalog.GetConcreteDocumentTypes(PaymentDocumentCatalog.MessagingAssembly);

            var withoutConstructor = documentTypes
                .Where(t => t.GetConstructor(Type.EmptyTypes) == null)
                .Select(t => t.FullName)
                .ToArray();

            documentTypes.ShouldNotBeEmpty();
            withoutConstructor.ShouldBeEmpty();
        }

        [TestCaseSource(nameof(DocumentCases))]
        public void TryGetTypeForMediaType_PaymentDocumentMimeType_ResolvesToItsClass(Type documentType, string mimeType)
        {
            var target = GetTarget();

            var found = target.TryGetTypeForMediaType(MediaType.Parse(mimeType), out var resolvedType);

            found.ShouldBeTrue();
            resolvedType.ShouldBe(documentType);
        }

        [Test]
        public void TryGetTypeForMediaType_CheckoutDocumentMimeType_ResolvesToCheckoutDocument()
        {
            var target = GetTarget();

            var found = target.TryGetTypeForMediaType(MediaType.Parse("application/vnd.iris.payment.checkout-link-request+json"), out var resolvedType);

            found.ShouldBeTrue();
            resolvedType.ShouldBe(typeof(CheckoutDocument));
        }

        [Test]
        public void TryGetTypeForMediaType_UpperCaseMimeType_IsNotResolved()
        {
            var target = GetTarget();

            var found = target.TryGetTypeForMediaType(MediaType.Parse(PaymentCheckoutRequest.MIME_TYPE.ToUpperInvariant()), out _);

            found.ShouldBeFalse();
        }

        private static object[] DocumentCases() =>
            PaymentDocumentCatalog.DocumentMimeTypes
                .Select(p => new object[] { p.Key, p.Value })
                .ToArray<object>();
    }
}

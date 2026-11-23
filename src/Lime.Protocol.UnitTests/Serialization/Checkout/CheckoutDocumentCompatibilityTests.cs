using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Lime.Messaging.Resources.Checkout;
using NUnit.Framework;
using Shouldly;
using static Lime.Protocol.UnitTests.Serialization.Checkout.PaymentDocumentFixtures;

#pragma warning disable CS0618 // The obsolete document is the subject of these tests.

namespace Lime.Protocol.UnitTests.Serialization.Checkout
{
    [TestFixture]
    public class CheckoutDocumentCompatibilityTests
    {
        private const string CheckoutLinkRequestMimeType = "application/vnd.iris.payment.checkout-link-request+json";

        private static readonly IReadOnlyDictionary<string, Type> PublishedMembers = new Dictionary<string, Type>
        {
            { "customer", typeof(CustomerCheckout) },
            { "products", typeof(List<ProductCheckout>) },
            { "currency", typeof(string) },
            { "channel", typeof(string) },
            { "source", typeof(string) },
            { "paymentMethodsToGenerate", typeof(List<CheckoutPaymentMethod>) },
            { "botShortName", typeof(string) },
            { "merchantId", typeof(string) },
        };

        [Test]
        public void CheckoutDocument_MimeType_IsUnchanged()
        {
            CheckoutDocument.MIME_TYPE.ShouldBe(CheckoutLinkRequestMimeType);
            CheckoutDocument.MediaType.ToString().ShouldBe(CheckoutLinkRequestMimeType);
            new CheckoutDocument().GetMediaType().ToString().ShouldBe(CheckoutLinkRequestMimeType);
        }

        [Test]
        public void CheckoutDocument_Members_AreUnchanged()
        {
            var members = typeof(CheckoutDocument)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .ToDictionary(p => p.GetCustomAttribute<DataMemberAttribute>().Name, p => p.PropertyType);

            members.Count.ShouldBe(PublishedMembers.Count);
            foreach (var member in PublishedMembers)
            {
                members.ShouldContainKey(member.Key);
                members[member.Key].ShouldBe(member.Value, member.Key);
            }
        }

        [Test]
        public void CheckoutDocument_IsObsoleteWithoutBreakingCompilation()
        {
            var obsolete = typeof(CheckoutDocument).GetCustomAttribute<ObsoleteAttribute>();

            obsolete.ShouldNotBeNull();
            obsolete.IsError.ShouldBeFalse();
            obsolete.Message.ShouldContain(nameof(PaymentCheckoutRequest));
        }

        [TestCaseSource(nameof(SerializerAndKindCases))]
        public void Read_CheckoutLinkRequestMimeType_IsReadAsCheckoutDocument(string serializer, string kind)
        {
            var json = "{\"currency\":\"BRL\",\"channel\":\"whatsapp\",\"merchantId\":\"d4a8f1e2-6b3c-4c9d-a7e5-1f2b3c4d5e6f\",\"paymentMethodsToGenerate\":[\"Pix\"]}";

            var (read, written) = RoundTrip(serializer, kind, CheckoutLinkRequestMimeType, json);

            var document = read.ShouldBeOfType<CheckoutDocument>();
            document.Currency.ShouldBe("BRL");
            document.PaymentMethodsToGenerate.ShouldBe(new List<CheckoutPaymentMethod> { CheckoutPaymentMethod.Pix });
            written.Value<string>("type").ShouldBe(CheckoutLinkRequestMimeType);
        }

        private static IEnumerable<object[]> SerializerAndKindCases()
        {
            foreach (var serializer in Serializers)
            {
                foreach (var kind in EnvelopeKinds)
                {
                    yield return new object[] { serializer, kind };
                }
            }
        }
    }
}

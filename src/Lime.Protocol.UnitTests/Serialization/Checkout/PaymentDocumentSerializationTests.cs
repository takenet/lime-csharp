using System;
using System.Collections.Generic;
using Lime.Messaging.Resources.Checkout;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Shouldly;
using static Lime.Protocol.UnitTests.Serialization.Checkout.PaymentDocumentFixtures;

namespace Lime.Protocol.UnitTests.Serialization.Checkout
{
    [TestFixture]
    public class PaymentDocumentSerializationTests
    {
        private const string PixExpiresAt = "2026-12-01T12:00:00Z";
        private const string PixExpiresAtReadByNewtonsoft = "12/01/2026 12:00:00";

        [TestCaseSource(nameof(RoundTripCases))]
        public void ReadAndWrite_Fixture_ProducesSameJson(Type documentType, string fileName, string serializer, string kind)
        {
            var mimeType = PaymentDocumentCatalog.DocumentMimeTypes[documentType];
            var fixture = Read(fileName);

            var (read, written) = RoundTrip(serializer, kind, mimeType, fixture);

            read.ShouldBeOfType(documentType);
            written.Value<string>("type").ShouldBe(mimeType);
            var writtenDocument = GetDocumentToken(kind, written);
            var expected = ExpectedAfterRoundTrip(Parse(fixture), serializer);
            JToken.DeepEquals(writtenDocument, expected).ShouldBeTrue("written: " + writtenDocument);
        }

        [TestCaseSource(nameof(SerializerAndKindCases))]
        public void ReadAndWrite_PaymentCheckoutRequestFull_MapsEveryField(string serializer, string kind)
        {
            var (read, _) = RoundTrip(serializer, kind, PaymentCheckoutRequest.MIME_TYPE, Read("PaymentCheckoutRequest.full.json"));

            var request = read.ShouldBeOfType<PaymentCheckoutRequest>();
            request.ContractVersion.ShouldBe(1);
            request.IdempotencyKey.ShouldBe("6f0c2b1e9a514d8e8f0e3b7a2c9d1e44");
            request.Channel.ShouldBe(PaymentCheckoutRequest.CHANNEL_WHATSAPP);
            request.Currency.ShouldBe(PaymentCheckoutRequest.CURRENCY_BRL);
            request.Items.Length.ShouldBe(1);
            request.Items[0].Quantity.ShouldBe(1);
            request.Items[0].Amount.ShouldBe(4990L);
            request.PaymentMethods.ShouldBe(new[] { PaymentCheckoutRequest.PAYMENT_METHOD_PAYMENT_LINK, PaymentCheckoutRequest.PAYMENT_METHOD_PIX });
            request.LinkMessages.Mode.ShouldBe(PaymentStatusMessages.MODE_DEFAULT);
            request.PixMessages.Mode.ShouldBe(PaymentStatusMessages.MODE_CUSTOM);
            request.PixMessages.Expired.ShouldBe("Your Pix code expired.");
            request.ProductType.ShouldBe(PaymentCheckoutRequest.PRODUCT_TYPE_DIGITAL);
        }

        [TestCaseSource(nameof(EnvelopeKindCases))]
        public void Read_PaymentCheckoutResultFullWithSystemTextJson_KeepsPixExpirationText(string kind)
        {
            var (read, _) = RoundTrip(SystemTextJsonName, kind, PaymentCheckoutResult.MIME_TYPE, Read("PaymentCheckoutResult.full.json"));

            var result = read.ShouldBeOfType<PaymentCheckoutResult>();
            result.Status.ShouldBe(PaymentCheckoutResult.STATUS_CREATED);
            result.Pix.ExpiresAt.ShouldBe(PixExpiresAt);
            result.Pix.Key.ShouldBe("12345678000199");
        }

        [TestCaseSource(nameof(EnvelopeKindCases))]
        public void Read_PaymentCheckoutResultFullWithNewtonsoft_ReformatsPixExpirationText(string kind)
        {
            var (read, written) = RoundTrip(NewtonsoftName, kind, PaymentCheckoutResult.MIME_TYPE, Read("PaymentCheckoutResult.full.json"));

            var result = read.ShouldBeOfType<PaymentCheckoutResult>();
            result.Status.ShouldBe(PaymentCheckoutResult.STATUS_CREATED);
            result.Pix.ExpiresAt.ShouldBe(PixExpiresAtReadByNewtonsoft);
            result.Pix.Key.ShouldBe("12345678000199");
            GetDocumentToken(kind, written)["pix"].Value<string>("expiresAt").ShouldBe(PixExpiresAtReadByNewtonsoft);
        }

        [Test]
        public void Write_PaymentCheckoutResultWithNewtonsoft_KeepsPixExpirationText()
        {
            var serializer = CreateSerializer(NewtonsoftName);
            var command = new Command
            {
                Id = "1",
                Method = CommandMethod.Set,
                Uri = new LimeUri("/checkout"),
                Resource = new PaymentCheckoutResult { Pix = new PaymentCheckoutPix { ExpiresAt = PixExpiresAt } },
            };

            var written = Parse(serializer.Serialize(command));

            written["resource"]["pix"].Value<string>("expiresAt").ShouldBe(PixExpiresAt);
        }

        [TestCaseSource(nameof(SerializerAndKindCases))]
        public void ReadAndWrite_AbsentNullableFields_StayAbsent(string serializer, string kind)
        {
            var fixture = (JObject)Parse(Read("PaymentCheckoutRequest.minimal.json"));
            ((JObject)fixture["items"][0]).Remove("amount");

            var (read, written) = RoundTrip(serializer, kind, PaymentCheckoutRequest.MIME_TYPE, fixture.ToString());

            var request = read.ShouldBeOfType<PaymentCheckoutRequest>();
            request.ProductType.ShouldBeNull();
            request.LinkMessages.ShouldBeNull();
            request.Items[0].Amount.ShouldBeNull();
            var writtenDocument = (JObject)GetDocumentToken(kind, written);
            writtenDocument.ContainsKey("productType").ShouldBeFalse();
            writtenDocument.ContainsKey("linkMessages").ShouldBeFalse();
            ((JObject)writtenDocument["items"][0]).ContainsKey("amount").ShouldBeFalse();
            JToken.DeepEquals(writtenDocument, fixture).ShouldBeTrue("written: " + writtenDocument);
        }

        [TestCaseSource(nameof(SerializerAndKindCases))]
        public void ReadAndWrite_AbsentNullableBoolean_IsNotWrittenAsFalse(string serializer, string kind)
        {
            var (read, written) = RoundTrip(serializer, kind, CustomerLastCardResult.MIME_TYPE, "{\"contractVersion\":1}");

            read.ShouldBeOfType<CustomerLastCardResult>().HasCard.ShouldBeNull();
            var writtenDocument = (JObject)GetDocumentToken(kind, written);
            writtenDocument.ContainsKey("hasCard").ShouldBeFalse();
            writtenDocument.Count.ShouldBe(1);
        }

        [TestCaseSource(nameof(SerializerAndKindCases))]
        public void ReadAndWrite_NewerContractWithUnknownField_KeepsVersionAndDropsField(string serializer, string kind)
        {
            var fixture = (JObject)Parse(Read("SavedCardPaymentRequest.full.json"));
            fixture["contractVersion"] = 2;
            fixture["giftWrap"] = true;

            var (read, written) = RoundTrip(serializer, kind, SavedCardPaymentRequest.MIME_TYPE, fixture.ToString());

            read.ShouldBeOfType<SavedCardPaymentRequest>().ContractVersion.ShouldBe(2);
            var writtenDocument = (JObject)GetDocumentToken(kind, written);
            writtenDocument.Value<int>("contractVersion").ShouldBe(2);
            writtenDocument.ContainsKey("giftWrap").ShouldBeFalse();
            fixture.Remove("giftWrap");
            JToken.DeepEquals(writtenDocument, fixture).ShouldBeTrue("written: " + writtenDocument);
        }

        private static JToken ExpectedAfterRoundTrip(JToken fixture, string serializer)
        {
            var expected = fixture.DeepClone();
            var expiresAt = expected.SelectToken("pix.expiresAt");
            if (serializer == NewtonsoftName && expiresAt != null)
            {
                expiresAt.Replace(PixExpiresAtReadByNewtonsoft);
            }

            return expected;
        }

        private static IEnumerable<object[]> RoundTripCases()
        {
            foreach (var prefix in FixturePrefixes)
            {
                foreach (var variant in new[] { ".minimal.json", ".full.json" })
                {
                    foreach (var serializer in Serializers)
                    {
                        foreach (var kind in EnvelopeKinds)
                        {
                            yield return new object[] { prefix.Key, prefix.Value + variant, serializer, kind };
                        }
                    }
                }
            }
        }

        private static IEnumerable<object[]> EnvelopeKindCases()
        {
            foreach (var kind in EnvelopeKinds)
            {
                yield return new object[] { kind };
            }
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

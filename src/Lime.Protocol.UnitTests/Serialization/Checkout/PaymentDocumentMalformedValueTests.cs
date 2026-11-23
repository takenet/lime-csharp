using System;
using System.Collections.Generic;
using Lime.Messaging.Resources.Checkout;
using NUnit.Framework;
using Shouldly;
using static Lime.Protocol.UnitTests.Serialization.Checkout.PaymentDocumentFixtures;

namespace Lime.Protocol.UnitTests.Serialization.Checkout
{
    [TestFixture]
    public class PaymentDocumentMalformedValueTests
    {
        [TestCaseSource(nameof(NewtonsoftCases))]
        public void Read_MalformedValueWithNewtonsoft_ConvertsIntoTheClass(string fileName, Type documentType, string kind, Func<Document, object> getValue, object expected)
        {
            var mimeType = PaymentDocumentCatalog.DocumentMimeTypes[documentType];

            var (read, _) = RoundTrip(NewtonsoftName, kind, mimeType, Read(fileName));

            read.ShouldBeOfType(documentType);
            getValue(read).ShouldBe(expected);
        }

        [TestCaseSource(nameof(SystemTextJsonCases))]
        public void Read_MalformedValueWithSystemTextJson_FallsBackToJsonDocument(string fileName, Type documentType, string kind)
        {
            var mimeType = PaymentDocumentCatalog.DocumentMimeTypes[documentType];

            var (read, written) = RoundTrip(SystemTextJsonName, kind, mimeType, Read(fileName));

            read.ShouldBeOfType<JsonDocument>();
            read.GetMediaType().ToString().ShouldBe(mimeType);
            written.Value<string>("type").ShouldBe(mimeType);
        }

        private static readonly Func<Document, object> ItemAmount = d => ((PaymentCheckoutRequest)d).Items[0].Amount;
        private static readonly Func<Document, object> ItemQuantity = d => ((PaymentCheckoutRequest)d).Items[0].Quantity;
        private static readonly Func<Document, object> HasCard = d => ((CustomerLastCardResult)d).HasCard;

        private static IEnumerable<object[]> Malformed()
        {
            yield return new object[] { "PaymentCheckoutRequest.invalid-amount-string.json", typeof(PaymentCheckoutRequest), ItemAmount, (long?)4990 };
            yield return new object[] { "PaymentCheckoutRequest.invalid-amount-integral-decimal.json", typeof(PaymentCheckoutRequest), ItemAmount, (long?)4990 };
            yield return new object[] { "PaymentCheckoutRequest.invalid-amount-fraction.json", typeof(PaymentCheckoutRequest), ItemAmount, (long?)50 };
            yield return new object[] { "PaymentCheckoutRequest.invalid-quantity-fraction.json", typeof(PaymentCheckoutRequest), ItemQuantity, (int?)2 };
            yield return new object[] { "CustomerLastCardResult.invalid-has-card-string.json", typeof(CustomerLastCardResult), HasCard, (bool?)true };
        }

        private static IEnumerable<object[]> NewtonsoftCases()
        {
            foreach (var row in Malformed())
            {
                foreach (var kind in EnvelopeKinds)
                {
                    yield return new[] { row[0], row[1], kind, row[2], row[3] };
                }
            }
        }

        private static IEnumerable<object[]> SystemTextJsonCases()
        {
            foreach (var row in Malformed())
            {
                foreach (var kind in EnvelopeKinds)
                {
                    yield return new[] { row[0], row[1], kind };
                }
            }
        }
    }
}

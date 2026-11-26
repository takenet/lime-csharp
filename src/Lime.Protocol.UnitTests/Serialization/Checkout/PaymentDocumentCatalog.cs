using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lime.Messaging.Resources.Checkout;

namespace Lime.Protocol.UnitTests.Serialization.Checkout
{
    /// <summary>
    /// Lists the payment checkout types and provides the helpers shared by their tests.
    /// </summary>
    internal static class PaymentDocumentCatalog
    {
        public static readonly IReadOnlyDictionary<Type, string> DocumentMimeTypes = new Dictionary<Type, string>
        {
            { typeof(PaymentCheckoutRequest), "application/vnd.iris.payment.checkout-request+json" },
            { typeof(PaymentCheckoutResult), "application/vnd.iris.payment.checkout-result+json" },
            { typeof(CustomerLastCardRequest), "application/vnd.iris.payment.customer-last-card-request+json" },
            { typeof(CustomerLastCardResult), "application/vnd.iris.payment.customer-last-card-result+json" },
            { typeof(SavedCardPaymentRequest), "application/vnd.iris.payment.saved-card-payment-request+json" },
            { typeof(SavedCardPaymentResult), "application/vnd.iris.payment.saved-card-payment-result+json" },
        };

        public static readonly Type[] NestedTypes =
        {
            typeof(PaymentCheckoutItem),
            typeof(PaymentStatusMessages),
            typeof(PaymentCheckoutPix),
        };

        public static IEnumerable<Type> DocumentTypes => DocumentMimeTypes.Keys;

        public static IEnumerable<Type> AllTypes => DocumentTypes.Concat(NestedTypes);

        public static Assembly MessagingAssembly => typeof(Lime.Messaging.DocumentTypeResolverExtensions).Assembly;

        public static Type[] GetConcreteDocumentTypes(Assembly assembly) =>
            assembly
                .DefinedTypes
                .Where(t => !t.IsAbstract && typeof(Document).IsAssignableFrom(t))
                .Select(t => t.AsType())
                .ToArray();

        public static string GetMimeType(Type documentType) =>
            ((Document)Activator.CreateInstance(documentType)).GetMediaType().ToString();

        public static string[] FindDuplicates(IEnumerable<string> mimeTypes) =>
            mimeTypes
                .GroupBy(m => m, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToArray();

        public static string[] FindWithUpperCase(IEnumerable<string> mimeTypes) =>
            mimeTypes
                .Where(m => !string.Equals(m, m.ToLowerInvariant(), StringComparison.Ordinal))
                .ToArray();
    }
}

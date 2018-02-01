using System.Globalization;
using Lime.Protocol.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Lime.Protocol.UnitTests.Serialization
{
    [TestClass]
    public class TypeUtilTests
    {
        [TestMethod]
        public void GetGenericFormatterParseFunc_DoubleInvariantCulture_ReturnValidParser()
        {
            // Act
            var parseFunc = TypeUtilEx.GetFormattedParseFunc<double>();
            var result = parseFunc("100.000", CultureInfo.InvariantCulture);

            // Assert
            result.ShouldBe(100);
        }

        [TestMethod]
        public void GetGenericFormatterParseFunc_DoubleSpecificCulture_ReturnValidParser()
        {
            // Act
            var parseFunc = TypeUtilEx.GetFormattedParseFunc<double>();
            var result = parseFunc("100.000", CultureInfo.GetCultureInfo("pt-BR"));

            // Assert
            result.ShouldBe(100_000);
        }

        [TestMethod]
        public void GetFormatterParseFunc_DoubleInvariantCulture_ReturnValidParser()
        {
            // Act
            var parseFunc = TypeUtilEx.GetFormattedParseFuncForType(typeof(double));
            var result = parseFunc("100.000", CultureInfo.InvariantCulture);

            // Assert
            result.ShouldBe(100);
        }

        [TestMethod]
        public void GetFormatterParseFunc_DoubleSpecificCulture_ReturnValidParser()
        {
            // Act
            var parseFunc = TypeUtilEx.GetFormattedParseFuncForType(typeof(double));
            var result = parseFunc("100.000", CultureInfo.GetCultureInfo("pt-BR"));

            // Assert
            result.ShouldBe(100_000);
        }
    }
}

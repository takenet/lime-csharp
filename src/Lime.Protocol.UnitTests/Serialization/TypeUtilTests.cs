using System.Globalization;
using Lime.Protocol.Serialization;
using Shouldly;
using Xunit;

namespace Lime.Protocol.UnitTests.Serialization
{
    public class TypeUtilTests
    {
        [Fact]
        public void GetGenericFormatterParseFunc_DoubleInvariantCulture_ReturnValidParser()
        {
            // Act
            var parseFunc = TypeUtilEx.GetFormattedParseFunc<double>();
            var result = parseFunc("100.000", CultureInfo.InvariantCulture);

            // Assert
            result.ShouldBe(100);
        }

        [Fact]
        public void GetGenericFormatterParseFunc_DoubleSpecificCulture_ReturnValidParser()
        {
            // Act
            var parseFunc = TypeUtilEx.GetFormattedParseFunc<double>();
            var result = parseFunc("100.000", CultureInfo.GetCultureInfo("pt-BR"));

            // Assert
            result.ShouldBe(100_000);
        }

        [Fact]
        public void GetFormatterParseFunc_DoubleInvariantCulture_ReturnValidParser()
        {
            // Act
            var parseFunc = TypeUtilEx.GetFormattedParseFuncForType(typeof(double));
            var result = parseFunc("100.000", CultureInfo.InvariantCulture);

            // Assert
            result.ShouldBe(100);
        }

        [Fact]
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

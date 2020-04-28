using System.Globalization;
using Lime.Protocol.Serialization;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests.Serialization
{
    [TestFixture]
    public class TypeUtilTests
    {
        [Test]
        public void GetGenericFormatterParseFunc_DoubleInvariantCulture_ReturnValidParser()
        {
            // Act
            var parseFunc = TypeUtilEx.GetFormattedParseFunc<double>();
            var result = parseFunc("100.000", CultureInfo.InvariantCulture);

            // Assert
            result.ShouldBe(100);
        }

        [Test]
        public void GetGenericFormatterParseFunc_DoubleSpecificCulture_ReturnValidParser()
        {
            // Act
            var parseFunc = TypeUtilEx.GetFormattedParseFunc<double>();
            var result = parseFunc("100.000", CultureInfo.GetCultureInfo("pt-BR"));

            // Assert
            result.ShouldBe(100_000);
        }

        [Test]
        public void GetFormatterParseFunc_DoubleInvariantCulture_ReturnValidParser()
        {
            // Act
            var parseFunc = TypeUtilEx.GetFormattedParseFuncForType(typeof(double));
            var result = parseFunc("100.000", CultureInfo.InvariantCulture);

            // Assert
            result.ShouldBe(100);
        }

        [Test]
        public void GetFormatterParseFunc_DoubleSpecificCulture_ReturnValidParser()
        {
            // Act
            var parseFunc = TypeUtilEx.GetFormattedParseFuncForType(typeof(double));
            var result = parseFunc("100.000", CultureInfo.GetCultureInfo("pt-BR"));

            // Assert
            result.ShouldBe(100_000);
        }

        [Test]
        public void GetStringValue_EmptyStringToEmptyArray_ReturnEmptyArray()
        {
            object result = null;
            //Act
            TypeUtilEx.TryParseString(string.Empty, typeof(string[]), out result, CultureInfo.InvariantCulture);

            //Assert
            var output = result.ShouldBeOfType<string[]>();
            output.ShouldBeEmpty();
        }
    }
}

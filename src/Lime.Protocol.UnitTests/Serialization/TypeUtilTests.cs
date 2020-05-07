using System;
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
            //Act
            var actual = TypeUtilEx.TryParseString(string.Empty, typeof(string[]), out var result, CultureInfo.InvariantCulture, StringSplitOptions.RemoveEmptyEntries);

            //Assert
            actual.ShouldBeTrue();
            var output = result.ShouldBeOfType<string[]>();
            output.ShouldBeEmpty();
        }
        
        [Test]
        public void GetStringValue_EmptyStringToEmptyArray_ReturnArray()
        {
            //Act
            var actual = TypeUtilEx.TryParseString(string.Empty, typeof(string[]), out var result, CultureInfo.InvariantCulture, StringSplitOptions.None);

            //Assert
            actual.ShouldBeTrue();
            var output = result.ShouldBeOfType<string[]>();
            output.Length.ShouldBe(1);
            output[0].ShouldBe("");
            
        }
    }
}

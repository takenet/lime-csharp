using System;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests
{
    [TestFixture]
    public class LimeUriTests
    {        
        [Test]
        public void Parse_ValidRelativeString_ReturnsInstance()
        {
            var resourceName = DataUtil.CreateRandomString(10);
            var relativePath = string.Format("/{0}", resourceName);
            var actual = LimeUri.Parse(relativePath);

            actual.Path.ShouldNotBe(null);
            actual.Path.ShouldBe(relativePath);
            actual.IsRelative.ShouldBe(true);
        }

        [Test]
        public void Parse_ValidAbsoluteString_ReturnsInstance()
        {
            var identity = DataUtil.CreateIdentity();
            var resourceName = DataUtil.CreateRandomString(10);
            var absolutePath = string.Format("{0}://{1}/{2}", LimeUri.LIME_URI_SCHEME, identity, resourceName);
            var actual = LimeUri.Parse(absolutePath);

            actual.Path.ShouldNotBe(null);
            actual.Path.ShouldBe(absolutePath);
            actual.IsRelative.ShouldBe(false);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Parse_NullString_ThrowsArgumentNullException()
        {
            string path = null;
            var actual = LimeUri.Parse(path);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Parse_InvalidRelativeString_ThrowsArgumentException()
        {
            var resourceName = DataUtil.CreateRandomString(10);
            var invalidPath = string.Format("\\{0}", resourceName);            
            var actual = LimeUri.Parse(invalidPath);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Parse_InvalidSchemeAbsoluteString_ThrowsArgumentException()
        {
            var absolutePath = "http://server@limeprotocol.org/presence";
            var actual = LimeUri.Parse(absolutePath);
        }

        [Test]
        public void ToUri_AbsoluteInstance_ReturnsUri()
        {
            var identity = DataUtil.CreateIdentity();
            var resourceName = DataUtil.CreateRandomString(10);
            var absolutePath = string.Format("{0}://{1}/{2}", LimeUri.LIME_URI_SCHEME, identity, resourceName);
            var limeUri = LimeUri.Parse(absolutePath);
            
            // Act
            var uri = limeUri.ToUri();

            // Assert
            uri.Scheme.ShouldBe(LimeUri.LIME_URI_SCHEME);
            uri.UserInfo.ShouldBe(identity.Name);
            uri.Authority.ShouldBe(identity.Domain);
            uri.PathAndQuery.ShouldBe("/" + resourceName);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ToUri_RelativeInstance_ThrowsInvalidOperationException()
        {
            var resourceName = DataUtil.CreateRandomString(10);
            var relativePath = string.Format("/{0}", resourceName);
            var limeUri = LimeUri.Parse(relativePath);

            // Act
            var uri = limeUri.ToUri();
        }


        [Test]
        public void ToUriIdentity_RelativeInstance_ReturnsUri()
        {
            var identity = DataUtil.CreateIdentity();

            var resourceName = DataUtil.CreateRandomString(10);
            var relativePath = string.Format("/{0}", resourceName);

            var limeUri = LimeUri.Parse(relativePath);

            // Act
            var uri = limeUri.ToUri(identity);

            // Assert
            uri.Scheme.ShouldBe(LimeUri.LIME_URI_SCHEME);
            uri.UserInfo.ShouldBe(identity.Name);
            uri.Authority.ShouldBe(identity.Domain);
            uri.PathAndQuery.ShouldBe("/" + resourceName);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ToUriIdentity_AbsoluteInstance_ThrowsInvalidOperationException()
        {
            var identity = DataUtil.CreateIdentity();
            var resourceName = DataUtil.CreateRandomString(10);
            var absolutePath = string.Format("{0}://{1}/{2}", LimeUri.LIME_URI_SCHEME, identity, resourceName);
            var limeUri = LimeUri.Parse(absolutePath);

            // Act
            var uri = limeUri.ToUri(identity);
        }

    }
}

using System;
using System.Linq;
using Xunit;
using Shouldly;

namespace Lime.Protocol.UnitTests
{
    
    public class IdentityTests
    {
        #region Equals

        [Fact]
        [Trait("Category", "Equals")]
        public void Equals_EqualsIdentities_ReturnsTrue()
        {
            var identity1 = new Identity()
            {
                Name = Dummy.CreateRandomString(10),
                Domain = Dummy.CreateRandomString(10)
            };

            var identity2 = new Identity()
            {
                Name = identity1.Name,
                Domain = identity1.Domain
            };


            Assert.Equal(identity1, identity2);
            Assert.Equal(identity2, identity1);
        }

        [Fact]
        [Trait("Category", "Equals")]
        public void Equals_EqualsIdentitiesDifferentCasing_ReturnsTrue()
        {
            var identity1 = new Identity()
            {
                Name = Dummy.CreateRandomString(10).ToUpper(),
                Domain = Dummy.CreateRandomString(10).ToUpper()
            };

            var identity2 = new Identity()
            {
                Name = identity1.Name.ToLower(),
                Domain = identity1.Domain.ToLower()
            };

            Assert.Equal(identity1, identity2);
            Assert.Equal(identity2, identity1);

        }

        [Fact]
        [Trait("Category", "Equals")]
        public void Equals_EqualsIdentitiesNullDomain_ReturnsTrue()
        {
            var identity1 = new Identity()
            {
                Name = Dummy.CreateRandomString(10),
                Domain = null
            };

            var identity2 = new Identity()
            {
                Name = identity1.Name,
                Domain = null
            };

            Assert.Equal(identity1, identity2);
            Assert.Equal(identity2, identity1);
        }

        [Fact]
        [Trait("Category", "Equals")]
        public void Equals_EqualsIdentitiesNullName_ReturnsTrue()
        {
            var identity1 = new Identity()
            {
                Name = null,
                Domain = Dummy.CreateRandomString(10)
            };

            var identity2 = new Identity()
            {
                Name = null,
                Domain = identity1.Domain
            };


            Assert.Equal(identity1, identity2);
            Assert.Equal(identity2, identity1);
        }

        [Fact]
        [Trait("Category", "Equals")]
        public void Equals_NotEqualsIdentities_ReturnsFalse()
        {
            var identity1 = new Identity()
            {
                Name = Dummy.CreateRandomString(10),
                Domain = Dummy.CreateRandomString(10)
            };

            var identity2 = new Identity()
            {
                Name = Dummy.CreateRandomString(11),
                Domain = Dummy.CreateRandomString(10)
            };


            Assert.NotEqual(identity1, identity2);
            Assert.NotEqual(identity2, identity1);
        }

        [Fact]
        [Trait("Category", "Equals")]
        public void Equals_NotEqualsIdentitiesNullName_ReturnsFalse()
        {
            var identity1 = new Identity()
            {
                Name = null,
                Domain = Dummy.CreateRandomString(10)
            };

            var identity2 = new Identity()
            {
                Name = Dummy.CreateRandomString(10),
                Domain = Dummy.CreateRandomString(10)
            };


            Assert.NotEqual(identity1, identity2);
            Assert.NotEqual(identity2, identity1);
        }

        [Fact]
        [Trait("Category", "Equals")]
        public void Equals_NotEqualsIdentitiesNullDomain_ReturnsFalse()
        {
            var identity1 = new Identity()
            {
                Name = Dummy.CreateRandomString(10),
                Domain = null
            };

            var identity2 = new Identity()
            {
                Name = Dummy.CreateRandomString(10),
                Domain = Dummy.CreateRandomString(10)
            };


            Assert.NotEqual(identity1, identity2);
            Assert.NotEqual(identity2, identity1);
        }

        [Fact]
        [Trait("Category", "Equals")]
        public void Equals_NotEqualsIdentitiesNullProperties_ReturnsFalse()
        {
            var identity1 = new Identity()
            {
                Name = null,
                Domain = null
            };

            var identity2 = new Identity()
            {
                Name = Dummy.CreateRandomString(10),
                Domain = Dummy.CreateRandomString(10)
            };


            Assert.NotEqual(identity1, identity2);
            Assert.NotEqual(identity2, identity1);
        }

        #endregion

        #region GetHashCode

        [Fact]
        [Trait("Category", "GetHashCode")]
        public void GetHashCode_EqualsIdentities_ReturnsSameHash()
        {
            var identity1 = new Identity()
            {
                Name = Dummy.CreateRandomString(10),
                Domain = Dummy.CreateRandomString(10)
            };

            var identity2 = new Identity()
            {
                Name = identity1.Name,
                Domain = identity1.Domain
            };


            Assert.Equal(identity1.GetHashCode(), identity2.GetHashCode());
        }

        [Fact]
        [Trait("Category", "GetHashCode")]
        public void GetHashCode_EqualsIdentitiesDifferentCasing_ReturnsSameHash()
        {
            var identity1 = new Identity()
            {
                Name = Dummy.CreateRandomString(10).ToUpper(),
                Domain = Dummy.CreateRandomString(10).ToUpper()
            };

            var identity2 = new Identity()
            {
                Name = identity1.Name.ToLower(),
                Domain = identity1.Domain.ToLower()
            };


            Assert.Equal(identity1.GetHashCode(), identity2.GetHashCode());
        }

        [Fact]
        [Trait("Category", "GetHashCode")]
        public void GetHashCode_NotEqualsIdentities_ReturnsDifferentHash()
        {
            var identity1 = new Identity()
            {
                Name = Dummy.CreateRandomString(10),
                Domain = Dummy.CreateRandomString(10)
            };

            var identity2 = new Identity()
            {
                Name = new string(identity1.Name.Reverse().ToArray()) + Dummy.CreateRandomString(5),
                Domain = new string(identity1.Domain.Reverse().ToArray()) + Dummy.CreateRandomString(5),
            };


            Assert.NotEqual(identity1.GetHashCode(), identity2.GetHashCode());
        }


        #endregion

        #region ToString

        [Fact]
        [Trait("Category", "ToString")]
        public void ToString_CompleteIdentity_ReturnsValidString()
        {
            var name = Dummy.CreateRandomString(10);
            var domain = Dummy.CreateRandomString(10);

            var identity1 = new Identity()
            {
                Name = name,
                Domain = domain
            };

            var expectedResult = string.Format("{0}@{1}", name, domain);

            Assert.Equal(identity1.ToString(), expectedResult);
        }

        [Fact]
        [Trait("Category", "ToString")]
        public void ToString_OnlyNameIdentity_ReturnsValidString()
        {
            var name = Dummy.CreateRandomString(10);
            string domain = null;

            var identity1 = new Identity()
            {
                Name = name,
                Domain = domain
            };

            var expectedResult = name;

            Assert.Equal(identity1.ToString(), expectedResult);
        }

        [Fact]
        [Trait("Category", "ToString")]
        public void ToString_OnlyDomainIdentity_ReturnsValidString()
        {
            string name = null;
            var domain = Dummy.CreateRandomString(10);

            var identity1 = new Identity()
            {
                Name = name,
                Domain = domain
            };

            var expectedResult = string.Format("@{0}", domain);

            Assert.Equal(identity1.ToString(), expectedResult);
        }

        #endregion

        #region Parse

        [Fact]
        [Trait("Category", "Parse")]
        public void Parse_CompleteString_ReturnsValidIdentity()
        {
            var name = Dummy.CreateRandomString(10);
            var domain = Dummy.CreateRandomString(10);

            var identityString = string.Format("{0}@{1}", name, domain);


            var identity = Identity.Parse(identityString);

            Assert.Equal(name, identity.Name);
            Assert.Equal(domain, identity.Domain);
        }

        [Fact]
        [Trait("Category", "Parse")]
        public void Parse_CompleteWithInstanceString_ReturnsValidIdentity()
        {
            var name = Dummy.CreateRandomString(10);
            var domain = Dummy.CreateRandomString(10);

            var identityString = string.Format("{0}@{1}/{2}", name, domain, "instance");


            var identity = Identity.Parse(identityString);

            Assert.Equal(name, identity.Name);
            Assert.Equal(domain, identity.Domain);
        }

        [Fact]
        [Trait("Category", "Parse")]
        public void Parse_OnlyNameString_ReturnsValidIdentity()
        {
            var name = Dummy.CreateRandomString(10);

            var identity = Identity.Parse(name);

            Assert.Equal(name, identity.Name);
            Assert.Null(identity.Domain);
        }

        [Fact]
        [Trait("Category", "Parse")]
        public void Parse_OnlyDomain_ReturnsValidIdentity()
        {
            var domain = Dummy.CreateRandomString(10);

            var identityString = string.Format("@{0}", domain);

            var identity = Identity.Parse(identityString);

            Assert.Equal(domain, identity.Domain);
            Assert.Null(identity.Name);
        }

        [Fact]
        [Trait("Category", "Parse")]
        public void Parse_OnlyAt_ReturnsValidIdentity()
        {
            var identityString = "@";

            var identity = Identity.Parse(identityString);

            Assert.Null(identity.Name);
            Assert.Null(identity.Domain);
        }

        [Fact]
        [Trait("Category", "Parse")]
        public void Parse_NullString_ThrowsArgumentNullException()
        {
            string identityString = null;

            Action action = () => Identity.Parse(identityString);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        [Trait("Category", "Parse")]
        public void Parse_EmptyString_ThrowsArgumentException()
        {
            string identityString = string.Empty;

            Action action = () => Identity.Parse(identityString);
            action.ShouldThrow<ArgumentException>();
        }

        #endregion

        #region Conversion

        [Fact]
        [Trait("Category", "Conversion")]
        public void Conversion_FromString_ReturnsValidIdentity()
        {
            // Act
            Identity identity = "name@domain.com";

            // Assert
            identity.Name.ShouldBe("name");
            identity.Domain.ShouldBe("domain.com");
        }

        [Fact]
        [Trait("Category", "Conversion")]
        public void Conversion_ToString_ReturnsValidIdentity()
        {
            // Act
            string identity = new Identity("name", "domain.com");

            // Assert
            identity.ShouldBe("name@domain.com");
        }

        #endregion
    }
}

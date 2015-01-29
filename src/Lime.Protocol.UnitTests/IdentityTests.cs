using System;
using System.Linq;
using NUnit.Framework;

namespace Lime.Protocol.UnitTests
{
    [TestFixture]
    public class IdentityTests
    {
        #region Equals

        [Test]
        [Category("Equals")]
        public void Equals_EqualsIdentities_ReturnsTrue()
        {
            var identity1 = new Identity()
            {
                Name = DataUtil.CreateRandomString(10),
                Domain = DataUtil.CreateRandomString(10)
            };

            var identity2 = new Identity()
            {
                Name = identity1.Name,
                Domain = identity1.Domain
            };


            Assert.AreEqual(identity1, identity2);
            Assert.AreEqual(identity2, identity1);
        }

        [Test]
        [Category("Equals")]
        public void Equals_EqualsIdentitiesDifferentCasing_ReturnsTrue()
        {
            var identity1 = new Identity()
            {
                Name = DataUtil.CreateRandomString(10).ToUpper(),
                Domain = DataUtil.CreateRandomString(10).ToUpper()
            };

            var identity2 = new Identity()
            {
                Name = identity1.Name.ToLower(),
                Domain = identity1.Domain.ToLower()
            };

            Assert.AreEqual(identity1, identity2);
            Assert.AreEqual(identity2, identity1);

        }

        [Test]
        [Category("Equals")]
        public void Equals_EqualsIdentitiesNullDomain_ReturnsTrue()
        {
            var identity1 = new Identity()
            {
                Name = DataUtil.CreateRandomString(10),
                Domain = null
            };

            var identity2 = new Identity()
            {
                Name = identity1.Name,
                Domain = null
            };

            Assert.AreEqual(identity1, identity2);
            Assert.AreEqual(identity2, identity1);
        }

        [Test]
        [Category("Equals")]
        public void Equals_EqualsIdentitiesNullName_ReturnsTrue()
        {
            var identity1 = new Identity()
            {
                Name = null,
                Domain = DataUtil.CreateRandomString(10)
            };

            var identity2 = new Identity()
            {
                Name = null,
                Domain = identity1.Domain
            };


            Assert.AreEqual(identity1, identity2);
            Assert.AreEqual(identity2, identity1);
        }

        [Test]
        [Category("Equals")]
        public void Equals_NotEqualsIdentities_ReturnsFalse()
        {
            var identity1 = new Identity()
            {
                Name = DataUtil.CreateRandomString(10),
                Domain = DataUtil.CreateRandomString(10)
            };

            var identity2 = new Identity()
            {
                Name = DataUtil.CreateRandomString(10),
                Domain = DataUtil.CreateRandomString(10)
            };


            Assert.AreNotEqual(identity1, identity2);
            Assert.AreNotEqual(identity2, identity1);
        }

        [Test]
        [Category("Equals")]
        public void Equals_NotEqualsIdentitiesNullName_ReturnsFalse()
        {
            var identity1 = new Identity()
            {
                Name = null,
                Domain = DataUtil.CreateRandomString(10)
            };

            var identity2 = new Identity()
            {
                Name = DataUtil.CreateRandomString(10),
                Domain = DataUtil.CreateRandomString(10)
            };


            Assert.AreNotEqual(identity1, identity2);
            Assert.AreNotEqual(identity2, identity1);
        }

        [Test]
        [Category("Equals")]
        public void Equals_NotEqualsIdentitiesNullDomain_ReturnsFalse()
        {
            var identity1 = new Identity()
            {
                Name = DataUtil.CreateRandomString(10),
                Domain = null
            };

            var identity2 = new Identity()
            {
                Name = DataUtil.CreateRandomString(10),
                Domain = DataUtil.CreateRandomString(10)
            };


            Assert.AreNotEqual(identity1, identity2);
            Assert.AreNotEqual(identity2, identity1);
        }

        [Test]
        [Category("Equals")]
        public void Equals_NotEqualsIdentitiesNullProperties_ReturnsFalse()
        {
            var identity1 = new Identity()
            {
                Name = null,
                Domain = null
            };

            var identity2 = new Identity()
            {
                Name = DataUtil.CreateRandomString(10),
                Domain = DataUtil.CreateRandomString(10)
            };


            Assert.AreNotEqual(identity1, identity2);
            Assert.AreNotEqual(identity2, identity1);
        }

        #endregion

        #region GetHashCode

        [Test]
        [Category("GetHashCode")]
        public void GetHashCode_EqualsIdentities_ReturnsSameHash()
        {
            var identity1 = new Identity()
            {
                Name = DataUtil.CreateRandomString(10),
                Domain = DataUtil.CreateRandomString(10)
            };

            var identity2 = new Identity()
            {
                Name = identity1.Name,
                Domain = identity1.Domain
            };


            Assert.AreEqual(identity1.GetHashCode(), identity2.GetHashCode());
        }

        [Test]
        [Category("GetHashCode")]
        public void GetHashCode_EqualsIdentitiesDifferentCasing_ReturnsSameHash()
        {
            var identity1 = new Identity()
            {
                Name = DataUtil.CreateRandomString(10).ToUpper(),
                Domain = DataUtil.CreateRandomString(10).ToUpper()
            };

            var identity2 = new Identity()
            {
                Name = identity1.Name.ToLower(),
                Domain = identity1.Domain.ToLower()
            };


            Assert.AreEqual(identity1.GetHashCode(), identity2.GetHashCode());
        }

        [Test]
        [Category("GetHashCode")]
        public void GetHashCode_NotEqualsIdentities_ReturnsDifferentHash()
        {
            var identity1 = new Identity()
            {
                Name = DataUtil.CreateRandomString(10),
                Domain = DataUtil.CreateRandomString(10)
            };

            var identity2 = new Identity()
            {
                Name = new string(identity1.Name.Reverse().ToArray()) + DataUtil.CreateRandomString(5),
                Domain = new string(identity1.Domain.Reverse().ToArray()) + DataUtil.CreateRandomString(5),
            };


            Assert.AreNotEqual(identity1.GetHashCode(), identity2.GetHashCode());
        }


        #endregion

        #region ToString

        [Test]
        [Category("ToString")]
        public void ToString_CompleteIdentity_ReturnsValidString()
        {
            var name = DataUtil.CreateRandomString(10);
            var domain = DataUtil.CreateRandomString(10);

            var identity1 = new Identity()
            {
                Name = name,
                Domain = domain
            };

            var expectedResult = string.Format("{0}@{1}", name, domain);

            Assert.AreEqual(identity1.ToString(), expectedResult);
        }

        [Test]
        [Category("ToString")]
        public void ToString_OnlyNameIdentity_ReturnsValidString()
        {
            var name = DataUtil.CreateRandomString(10);
            string domain = null;

            var identity1 = new Identity()
            {
                Name = name,
                Domain = domain
            };

            var expectedResult = name;

            Assert.AreEqual(identity1.ToString(), expectedResult);
        }

        [Test]
        [Category("ToString")]
        public void ToString_OnlyDomainIdentity_ReturnsValidString()
        {
            string name = null;
            var domain = DataUtil.CreateRandomString(10);

            var identity1 = new Identity()
            {
                Name = name,
                Domain = domain
            };

            var expectedResult = string.Format("@{0}", domain);

            Assert.AreEqual(identity1.ToString(), expectedResult);
        }

        #endregion

        #region Parse

        [Test]
        [Category("Parse")]
        public void Parse_CompleteString_ReturnsValidIdentity()
        {
            var name = DataUtil.CreateRandomString(10);
            var domain = DataUtil.CreateRandomString(10);

            var identityString = string.Format("{0}@{1}", name, domain);


            var identity = Identity.Parse(identityString);

            Assert.AreEqual(name, identity.Name);
            Assert.AreEqual(domain, identity.Domain);
        }

        [Test]
        [Category("Parse")]
        public void Parse_OnlyNameString_ReturnsValidIdentity()
        {
            var name = DataUtil.CreateRandomString(10);

            var identity = Identity.Parse(name);

            Assert.AreEqual(name, identity.Name);
            Assert.IsNull(identity.Domain);
        }

        [Test]
        [Category("Parse")]
        public void Parse_OnlyDomain_ReturnsValidIdentity()
        {
            var domain = DataUtil.CreateRandomString(10);

            var identityString = string.Format("@{0}", domain);

            var identity = Identity.Parse(identityString);

            Assert.AreEqual(domain, identity.Domain);
            Assert.IsNull(identity.Name);
        }


        [Test]
        [Category("Parse")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Parse_NullString_ThrowsArgumentNullException()
        {
            string identityString = null;

            var identity = Identity.Parse(identityString);
        }

        [Test]
        [Category("Parse")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Parse_EmptyString_ThrowsArgumentNullException()
        {
            string identityString = string.Empty;

            var identity = Identity.Parse(identityString);
        }

        #endregion
    }
}

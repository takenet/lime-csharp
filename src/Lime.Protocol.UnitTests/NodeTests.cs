using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shouldly;

namespace Lime.Protocol.UnitTests
{
    [TestFixture]
    public class NodeTests
    {
        #region Equals

        [Test]
        [Category("Equals")]
        public void Equals_EqualsNodes_ReturnsTrue()
        {
            var node1 = new Node
            {
                Name = Dummy.CreateRandomString(10),
                Domain = Dummy.CreateRandomString(10),
                Instance = Dummy.CreateRandomString(5)                
            };

            var node2 = new Node
            {
                Name = node1.Name,
                Domain = node1.Domain,
                Instance = node1.Instance
            };


            Assert.True(node1.Equals(node2));
            Assert.True(node2.Equals(node1));
            Assert.True(node1 == node2);
            Assert.True(node2 == node1);
        }

        [Test]
        [Category("Equals")]
        public void Equals_NodeEqualsNull_ReturnsFalse()
        {
            var node1 = new Node
            {
                Name = Dummy.CreateRandomString(10),
                Domain = Dummy.CreateRandomString(10),
                Instance = Dummy.CreateRandomString(5)
            };

            Node node2 = null;

            Assert.False(node1.Equals(node2));
            Assert.False(node1 == node2);
            Assert.False(node2 == node1);
        }

        #endregion

        #region NotEquals

        [Test]
        [Category("NotEquals")]
        public void NotEquals_NodeNotEqualsNull_ReturnsTrue()
        {
            var node1 = new Node
            {
                Name = Dummy.CreateRandomString(10),
                Domain = Dummy.CreateRandomString(10),
                Instance = Dummy.CreateRandomString(5)
            };

            Node node2 = null;
            
            Assert.True(!node1.Equals(node2));
            Assert.True(node1 != node2);
            Assert.True(node2 != node1);
        }

        #endregion

        #region Parse

        [Test]
        [Category("Parse")]
        public void Parse_CompleteString_ReturnsValidNode()
        {
            var name = Dummy.CreateRandomString(10);
            var domain = Dummy.CreateRandomString(10);
            var instance = Dummy.CreateRandomString(10);

            var nodeString = string.Format("{0}@{1}/{2}", name, domain, instance);

            var node = Node.Parse(nodeString);

            Assert.AreEqual(name, node.Name);
            Assert.AreEqual(domain, node.Domain);
            Assert.AreEqual(instance, node.Instance);
        }

        [Test]
        [Category("Parse")]
        public void Parse_WithoutInstance_ReturnsValidNode()
        {
            var name = Dummy.CreateRandomString(10);
            var domain = Dummy.CreateRandomString(10);

            var nodeString = string.Format("{0}@{1}", name, domain);

            var node = Node.Parse(nodeString);

            Assert.AreEqual(name, node.Name);
            Assert.AreEqual(domain, node.Domain);
            Assert.IsNull(node.Instance);
        }

        [Test]
        [Category("Parse")]
        public void Parse_WithEmptyInstance_ReturnsValidNode()
        {
            var name = Dummy.CreateRandomString(10);
            var domain = Dummy.CreateRandomString(10);

            var nodeString = string.Format("{0}@{1}/", name, domain);

            var node = Node.Parse(nodeString);

            Assert.AreEqual(name, node.Name);
            Assert.AreEqual(domain, node.Domain);
            Assert.AreEqual(string.Empty, node.Instance);
        }

        #endregion Parse

        #region Conversion

        [Test]
        [Category("Conversion")]
        public void Conversion_FromString_ReturnsValidNode()
        {
            // Act
            Node node = "name@domain.com/instance";

            // Assert
            node.Name.ShouldBe("name");
            node.Domain.ShouldBe("domain.com");
            node.Instance.ShouldBe("instance");
        }

        [Test]
        [Category("Conversion")]
        public void Conversion_ToString_ReturnsValidNode()
        {
            // Act
            string node = new Node("name", "domain.com", "instance");

            // Assert
            node.ShouldBe("name@domain.com/instance");
        } 

        #endregion
    }
}

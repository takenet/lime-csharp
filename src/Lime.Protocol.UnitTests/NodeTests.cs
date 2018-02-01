using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shouldly;

namespace Lime.Protocol.UnitTests
{
    [TestClass]
    public class NodeTests
    {
        #region Equals

        [TestMethod]
        [TestCategory("Equals")]
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


            Assert.IsTrue(node1.Equals(node2));
            Assert.IsTrue(node2.Equals(node1));
            Assert.IsTrue(node1 == node2);
            Assert.IsTrue(node2 == node1);
        }

        [TestMethod]
        [TestCategory("Equals")]
        public void Equals_NodeEqualsNull_ReturnsFalse()
        {
            var node1 = new Node
            {
                Name = Dummy.CreateRandomString(10),
                Domain = Dummy.CreateRandomString(10),
                Instance = Dummy.CreateRandomString(5)
            };

            Node node2 = null;

            Assert.IsFalse(node1.Equals(node2));
            Assert.IsFalse(node1 == node2);
            Assert.IsFalse(node2 == node1);
        }

        #endregion

        #region NotEquals

        [TestMethod]
        [TestCategory("NotEquals")]
        public void NotEquals_NodeNotEqualsNull_ReturnsTrue()
        {
            var node1 = new Node
            {
                Name = Dummy.CreateRandomString(10),
                Domain = Dummy.CreateRandomString(10),
                Instance = Dummy.CreateRandomString(5)
            };

            Node node2 = null;
            
            Assert.IsTrue(!node1.Equals(node2));
            Assert.IsTrue(node1 != node2);
            Assert.IsTrue(node2 != node1);
        }

        #endregion

        #region Parse

        [TestMethod]
        [TestCategory("Parse")]
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

        [TestMethod]
        [TestCategory("Parse")]
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

        [TestMethod]
        [TestCategory("Parse")]
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

        [TestMethod]
        [TestCategory("Conversion")]
        public void Conversion_FromString_ReturnsValidNode()
        {
            // Act
            Node node = "name@domain.com/instance";

            // Assert
            node.Name.ShouldBe("name");
            node.Domain.ShouldBe("domain.com");
            node.Instance.ShouldBe("instance");
        }

        [TestMethod]
        [TestCategory("Conversion")]
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

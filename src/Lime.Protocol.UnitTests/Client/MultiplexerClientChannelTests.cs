using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Client;
using Lime.Protocol.Network;
using Moq;
using NUnit.Framework;

namespace Lime.Protocol.UnitTests.Client
{
    [TestFixture]
    public class MultiplexerClientChannelTests
    {
        private Mock<IClientChannelBuilder> _clientChannelBuilder;
        private Mock<IEstablishedClientChannelBuilder> _establishedClientChannelBuilder;

        [SetUp]
        public void Setup()
        {

        }

    }
}

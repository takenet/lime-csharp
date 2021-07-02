using System;
using System.IO.Pipelines;
using System.Net;
using System.Threading.Tasks;
using Lime.Transport.AspNetCore.Middlewares;
using Microsoft.AspNetCore.Connections;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Lime.Transport.AspNetCore.UnitTests.Middlewares
{
    [TestFixture]
    public class TcpConnectionHandlerTests : TestsBase
    {
        [SetUp]
        public void SetUp()
        {
            base.SetUp(new TransportEndPoint()
            {
                Transport = TransportType.Tcp,
                EndPoint = new IPEndPoint(IPAddress.Any, 55321)
            });
            InputPipe = new Pipe();
            Reader = new FakePipeReader(InputPipe.Reader);
            OutputPipe = new Pipe();
            Writer = new FakePipeWriter(OutputPipe.Writer);
            
            Transport = new Mock<IDuplexPipe>();
            Transport
                .SetupGet(t => t.Input)
                .Returns(Reader);
            Transport
                .SetupGet(t => t.Output)
                .Returns(Writer);
            Context = new Mock<ConnectionContext>();
            Context
                .SetupGet(c => c.Transport)
                .Returns(Transport.Object);
            Context
                .SetupGet(c => c.LocalEndPoint)
                .Returns(TransportEndPoint.EndPoint);
            Context
                .SetupGet(c => c.ConnectionClosed)
                .Returns(CancellationTokenSource.Token);
        }
        
        public Mock<ConnectionContext> Context { get; set; }
        public Mock<IDuplexPipe> Transport { get; set; }
        public Pipe InputPipe { get; set; }
        public FakePipeReader Reader { get; set; }
        public Pipe OutputPipe { get; set; }
        public FakePipeWriter Writer { get; set; }
        
        
        private TcpConnectionHandler GetTarget() =>
            new TcpConnectionHandler(TransportListener, EnvelopeSerializer, Microsoft.Extensions.Options.Options.Create(Options));

        [Test]
        public async Task OnConnectedAsync_NewConnection_ShouldReadFromTransport()
        {
            // Arrange
            var target = GetTarget();
            Reader.ReadCalledHandler = () => CancellationTokenSource.Cancel(); // Close the connection when read is called
            
            // Act
            try
            {
                await target.OnConnectedAsync(Context.Object);
                throw new Exception("The listener was not cancelled");
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == CancellationTokenSource.Token)
            {
                
            }
            
            // Assert
            Reader.ReadCount.ShouldBeGreaterThanOrEqualTo(1);
        }
        
        [Test]
        public async Task OnConnectedAsync_ReadFailed_ShouldCloseTransport()
        {
            // Arrange
            var readException = new Exception("Cannot read from transport");
            Reader.ReadCalledHandler = () => throw readException;
            var target = GetTarget();
            
            // Act
            try
            {
                await target.OnConnectedAsync(Context.Object);
                throw new Exception("The listener was not cancelled");
            }
            catch (Exception ex) when (ex.Message == readException.Message)
            {
                
            }
            
            // Assert
            Reader.CompleteCalled.ShouldBeTrue();
            Writer.CompleteCalled.ShouldBeTrue();
        }

        [Test]
        public async Task OnConnectedAsync_UnexpectedEndPoint_ShouldDisposeConnection()
        {
            // Arrange
            var endPoint = new IPEndPoint(IPAddress.Any, 80);
            Context
                .SetupGet(c => c.LocalEndPoint)
                .Returns(endPoint);
            var target = GetTarget();

            // Act
            await target.OnConnectedAsync(Context.Object);
            
            // Assert
            Context.Verify(c => c.DisposeAsync(), Times.Once());
        }
    }
}
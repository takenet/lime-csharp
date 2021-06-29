using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Serialization;
using Lime.Transport.AspNetCore.Middlewares;
using Lime.Transport.AspNetCore.Transport;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Lime.Transport.AspNetCore.UnitTests.Middlewares
{
    [TestFixture]
    public class TcpConnectionHandlerTests
    {
        public TcpConnectionHandlerTests()
        {
            TransportEndPoint = new TransportEndPoint()
            {
                Transport = TransportType.Tcp,
                EndPoint = new IPEndPoint(IPAddress.Any, 55321)
            };
            LimeOptions = new LimeOptions()
            {
                EndPoints = new List<TransportEndPoint>()
                {
                    TransportEndPoint
                }
            };
            EnvelopeSerializer = new Mock<IEnvelopeSerializer>();
            ServiceScopeFactory = new Mock<IServiceScopeFactory>();
            Logger = new Logger<TransportListener>(new LoggerFactory());
            TransportListener = new TransportListener(
                Options.Create(LimeOptions), 
                ServiceScopeFactory.Object, 
                Logger);
            InputPipe = new Pipe();
            Reader = new PipeReaderDecorator(InputPipe.Reader);
            OutputPipe = new Pipe();
            Writer = new PipeWriterDecorator(OutputPipe.Writer);
            
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
            CancellationTokenSource = new CancellationTokenSource();
            Context
                .SetupGet(c => c.ConnectionClosed)
                .Returns(CancellationTokenSource.Token);
        }
        
        public TransportEndPoint TransportEndPoint { get; }
        internal TransportListener TransportListener { get; }
        public Mock<IEnvelopeSerializer> EnvelopeSerializer { get; }
        public LimeOptions LimeOptions { get; }
        public Mock<IServiceScopeFactory> ServiceScopeFactory { get; }
        internal ILogger<TransportListener> Logger { get; }
        public Mock<ConnectionContext> Context { get; }
        public Mock<IDuplexPipe> Transport { get; }
        public Pipe InputPipe { get; }
        public PipeReaderDecorator Reader { get; }
        public Pipe OutputPipe { get; }
        public PipeWriterDecorator Writer { get; } 
        
        public CancellationTokenSource CancellationTokenSource { get; }
        
        private TcpConnectionHandler GetTarget() =>
            new TcpConnectionHandler(TransportListener, EnvelopeSerializer.Object, Options.Create(LimeOptions));

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
            var target = GetTarget();
            Reader.ReadCalledHandler = () => throw readException;
            
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
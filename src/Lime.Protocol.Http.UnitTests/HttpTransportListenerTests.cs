using System;
using Lime.Protocol;
using Lime.Protocol.UnitTests;
using Lime.Protocol.Serialization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Net.Sockets;
using Shouldly;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Net;
using Lime.Protocol.Server;
using Lime.Protocol.Security;
using System.Text;


namespace Lime.Protocol.Http.UnitTests
{
    [TestClass]
    public class HttpTransportListenerTests
    {
        #region Public properties

        public string Host { get; private set; }

        public int Port { get; private set; }

        public HttpTransportListener Target { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public string User { get; private set; }

        public string Password { get; private set; }

        public HttpClient Client { get; private set; }

        public string MessagesUrl { get; private set; }

        public AuthenticationHeaderValue AuthenticationHeader { get; private set; }

        public HttpRequestMessage GetMessagesHttpRequestMessage { get; private set; }

        public TimeSpan SendTimeout { get; private set; }

        public Guid SessionId { get; private set; }

        public Node ClientNode { get; private set; }

        public Node ServerNode { get; private set; }

        public Reason Reason { get; private set; }

        public Message SentMessage { get; private set; }

        public HttpRequestMessage SendMessageHttpRequestMessage { get; private set; }


        public Notification Notification { get; set; }

        #endregion

        [TestInitialize]
        public void Arrange()
        {
            Host = "localhost";
            Port = 8080 + DataUtil.CreateRandomInt(10000);
            CancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();
            ClientNode = DataUtil.CreateNode();
            User = ClientNode.ToIdentity().ToString();
            Password = DataUtil.CreateRandomString(20);
            Client = new HttpClient();
            MessagesUrl = "http://" + Host + ":" + Port + "/messages";
            AuthenticationHeader = new AuthenticationHeaderValue("Basic", (User + ":" + Password).ToBase64());
            GetMessagesHttpRequestMessage = new HttpRequestMessage();
            GetMessagesHttpRequestMessage.Method = HttpMethod.Get;
            GetMessagesHttpRequestMessage.RequestUri = new Uri(MessagesUrl);
            GetMessagesHttpRequestMessage.Headers.Authorization = AuthenticationHeader;
            SessionId = Guid.NewGuid();
            ServerNode = DataUtil.CreateNode();
            SendTimeout = TimeSpan.FromSeconds(5);
            Reason = DataUtil.CreateReason();
            Target = new HttpTransportListener(Port, Host);

            SentMessage = new Message()
            {
                To = DataUtil.CreateNode(),
                Content = DataUtil.CreateTextContent()
            };            

            SendMessageHttpRequestMessage = new HttpRequestMessage();
            SendMessageHttpRequestMessage.Method = HttpMethod.Post;
            SendMessageHttpRequestMessage.RequestUri = new Uri(MessagesUrl);
            SendMessageHttpRequestMessage.Headers.Authorization = AuthenticationHeader;
            SendMessageHttpRequestMessage.Content = new StringContent(JsonSerializer.Serialize(SentMessage.Content), Encoding.UTF8, SentMessage.Content.GetMediaType().ToString());

        }

        [TestCleanup]
        public void Dispose()
        {
            if (Target != null)
            {
                Target.Dispose();
            }

            if (Client != null)
            {
                Client.Dispose();
            }
        }
        
        [TestMethod]
        public void ListenerUris_ValidHostAndPort_GetsRegisteredUris()
        {            
            // Act
            var listenerUris = Target.ListenerUris;

            // Assert
            listenerUris.ShouldNotBe(null);
            listenerUris.Length.ShouldBe(3);
            listenerUris.ShouldAllBe(u => u.Host == Host && u.Port == Port);
             
        }

        [TestMethod]
        public async Task StartAsync_ValidHostAndPort_StartsListening()
        {           
            // Act
            await Target.StartAsync();

            // Assert
            var client = new TcpClient();
            await client.ConnectAsync(Host, Port);
            client.Connected.ShouldBe(true);
            client.Close();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task StartAsync_CallTwice_ThrowsInvalidOperationException()
        {           
            // Act
            await Target.StartAsync();
            await Target.StartAsync();            
        }

        [TestMethod]
        public async Task AcceptTransportAsync_NewRequest_RetunsTransport()
        {
            // Act
            await Target.StartAsync();
            var httpResponseMessageTask = Client.SendAsync(GetMessagesHttpRequestMessage, CancellationToken);
            var transport = await Target.AcceptTransportAsync(CancellationToken);

            // Assert
            var serverHttpTransport = transport.ShouldBeOfType<ServerHttpTransport>();
            serverHttpTransport.HttpIdentity.ShouldNotBe(null);
            serverHttpTransport.HttpIdentity.Name.ShouldBe(User);
            serverHttpTransport.HttpIdentity.Password.ShouldBe(Password);           
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task AcceptTransportAsync_ListenerNotStarted_ThrowsInvalidOperationException()
        {
            // Act
            var transport = await Target.AcceptTransportAsync(CancellationToken);
        }

        [TestMethod]
        public async Task StopAsync_ActiveListener_StopsListening()
        {
            // Act
            await Target.StartAsync();
            await Target.StopAsync();

            // Assert
            try
            {
                var client = new TcpClient();
                await client.ConnectAsync(Host, Port);
            }
            catch (SocketException) { }            
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task StopAsync_ListenerNotStarted_ThrowsInvalidOperationException()
        {            
            // Act
            await Target.StopAsync();
        }

        [TestMethod]
        public async Task ProcessListenerContextAsync_ValidUrlAuthenticatedUser_CallProcessor()
        {
            // Act
            await Target.StartAsync();
            var httpResponseMessageTask = Client.SendAsync(GetMessagesHttpRequestMessage, CancellationToken);            
            var transport = await Target.AcceptTransportAsync(CancellationToken);

            var serverChannel = new ServerChannel(
                SessionId,
                ServerNode,
                transport,
                SendTimeout);

            await transport.OpenAsync(null, CancellationToken);
            await EstablishSessionAsync(serverChannel);
            var httpResponseMessage = await httpResponseMessageTask;

            // Assert
            httpResponseMessage.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        [TestMethod]
        public async Task ProcessListenerContextAsync_InvalidSessionAuthentication_ReturnsUnauthorized()
        {                 
            // Arrange
            var reason = Reason;
            reason.Code = ReasonCodes.SESSION_AUTHENTICATION_FAILED;

            // Act
            await Target.StartAsync();
            var httpResponseMessageTask = Client.SendAsync(GetMessagesHttpRequestMessage, CancellationToken);

            var transport = await Target.AcceptTransportAsync(CancellationToken);

            var serverChannel = new ServerChannel(
                SessionId,
                ServerNode,
                transport,
                SendTimeout);

            await transport.OpenAsync(null, CancellationToken);

            var receivedSession = await serverChannel.ReceiveNewSessionAsync(CancellationToken);
            if (receivedSession.State == SessionState.New)
            {
                await serverChannel.SendFailedSessionAsync(Reason);
            }

            var httpResponseMessage = await httpResponseMessageTask;

            // Assert
            httpResponseMessage.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            httpResponseMessage.ReasonPhrase.ShouldBe(reason.Description);
        }

        [TestMethod]
        public async Task ProcessListenerContextAsync_FailedSessionEmptyReason_ReturnsServiceUnavailable()
        {
            // Act
            await Target.StartAsync();
            var httpResponseMessageTask = Client.SendAsync(GetMessagesHttpRequestMessage, CancellationToken);

            var transport = await Target.AcceptTransportAsync(CancellationToken);

            var serverChannel = new ServerChannel(
                SessionId,
                ServerNode,
                transport,
                SendTimeout);

            await transport.OpenAsync(null, CancellationToken);

            var receivedSession = await serverChannel.ReceiveNewSessionAsync(CancellationToken);
            if (receivedSession.State == SessionState.New)
            {
                var failedSession = new Session()
                {
                    State = SessionState.Failed
                };

                await serverChannel.SendSessionAsync(failedSession);
            }

            var httpResponseMessage = await httpResponseMessageTask;

            // Assert
            httpResponseMessage.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        }

        [TestMethod]
        public async Task ProcessListenerContextAsync_RequestTimedOut_ReturnsTimeout()
        {
            // Act
            var target = new HttpTransportListener(Port, Host, requestTimeout: TimeSpan.FromMilliseconds(500));

            await target.StartAsync();
            var httpResponseMessageTask = Client.SendAsync(GetMessagesHttpRequestMessage, CancellationToken);


            await Task.Delay(500);

            var httpResponseMessage = await httpResponseMessageTask;

            // Assert
            httpResponseMessage.StatusCode.ShouldBe(HttpStatusCode.RequestTimeout);
        }

        [TestMethod]
        public async Task ProcessListenerContextAsync_InvalidUrl_ReturnsNotFound()
        {
            // Act
            var target = new HttpTransportListener(Port, Host, requestTimeout: TimeSpan.FromMilliseconds(500));            

            await target.StartAsync();
            GetMessagesHttpRequestMessage.RequestUri = new Uri(GetMessagesHttpRequestMessage.RequestUri, "/invalid/path");
            var httpResponseMessage = await Client.SendAsync(GetMessagesHttpRequestMessage, CancellationToken);
           
            // Assert
            httpResponseMessage.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task ProcessTransportOutputAsync_NotificationWithPendingResponse_CloseResponseWithSuccess()
        {
            // Act
            await Target.StartAsync();
            var httpResponseMessageTask = Client.SendAsync(SendMessageHttpRequestMessage, CancellationToken);
            var transport = await Target.AcceptTransportAsync(CancellationToken);
            var serverChannel = new ServerChannel(
                SessionId,
                ServerNode,
                transport,
                SendTimeout);
            await transport.OpenAsync(null, CancellationToken);
            await EstablishSessionAsync(serverChannel);

            

        }

        #region Private Methods

        private async Task EstablishSessionAsync(ServerChannel serverChannel)
        {
            var receivedSession = await serverChannel.ReceiveNewSessionAsync(CancellationToken);
            if (receivedSession.State == SessionState.New)
            {
                receivedSession = await serverChannel.AuthenticateSessionAsync(
                    new[] { AuthenticationScheme.Plain },
                    CancellationToken);

                if (receivedSession.State == SessionState.Authenticating)
                {
                    var plainAuthentication = receivedSession.Authentication as PlainAuthentication;
                    if (plainAuthentication != null &&
                        receivedSession.From.ToIdentity().Equals(ClientNode.ToIdentity()) &&
                        plainAuthentication.GetFromBase64Password().Equals(Password))
                    {
                        await serverChannel.SendEstablishedSessionAsync(ClientNode).ConfigureAwait(false);
                    }

                }

            }
        }

        #endregion
    }
}
LIME Protocol
=============

C# LIME protocol implementation

- Work in progress. See more at: http://limeprotocol.org/


### Nuget packages

- **Lime.Protocol**: Base protocol
- **Lime.Protocol.Tcp**: TCP transport


# Client

### Creating the channel

```csharp

// Creates a new transports and connect to the server
var serverUri = new Uri("net.tcp://localhost:55321");

var transport = new TcpTransport();
await transport.OpenAsync(serverUri, CancellationToken.None);

// Creates a new client channel
var sendTimeout = TimeSpan.FromSeconds(60);

var clientChannel = new ClientChannel(
    transport,
    sendTimeout);

```

### Session establishment

```csharp


// Option 1: Establish the session using a helper method
var clientNode = Node.Parse("client@domain.com/home");

var authentication = new PlainAuthentication();
authentication.SetToBase64Password("mypassword");

var session = await clientChannel.EstablishSessionAsync(
    (compressionOptions) => compressionOptions.First(),     // Compression selector 
    (encryptionOptions) => encryptionOptions.First(),       // Encryption selector
    clientNode.ToIdentity(),                                // Client identity
    (authenticationSchemes, roundtrip) => authentication,   // Authentication
    clientNode.Instance,
    CancellationToken.None);

if (session.State == SessionState.Established)
{
    // ...
}

// Option 2: Establish the session manually
// Sends the 'new' session envelope
var receivedSession = await clientChannel.StartNewSessionAsync(CancellationToken.None);

if (receivedSession.State == SessionState.Negotiating)
{
    // Choose the options and receive the confirmation
    receivedSession = await clientChannel.NegotiateSessionAsync(
        receivedSession.CompressionOptions.First(),
        receivedSession.EncryptionOptions.First(),
        CancellationToken.None);

    // Apply the options to the transport
    await transport.SetCompressionAsync(
        receivedSession.Compression.Value, 
        CancellationToken.None);
    await transport.SetEncryptionAsync(
    receivedSession.Encryption.Value, 
    CancellationToken.None);

    // Receive the authentication options
    receivedSession = await clientChannel.ReceiveAuthenticatingSessionAsync(CancellationToken.None);
}            

if (receivedSession.State == SessionState.Authenticating)
{
    // Authenticate the session
    receivedSession = await clientChannel.AuthenticateSessionAsync(
        clientNode.ToIdentity(),
        authentication,
        clientNode.Instance,
        CancellationToken.None);
}

if (session.State == SessionState.Established)
{
    // ...
}

// Requesting the session finishing
await clientChannel.SendFinishingSessionAsync();

// Awaits for the session end
receivedSession = await clientChannel.ReceiveFinishedSessionAsync(CancellationToken.None);

```

### Messages and notifications

```csharp

// Sending a text message
var message = new Message()
{
    To = Node.Parse("destionation@domain.com"),
    Content = new PlainText()
    {
        Text = "Hello!"
    }
};
await clientChannel.SendMessageAsync(message);
    
// Receive a message
var receivedMessage = await clientChannel.ReceiveMessageAsync(CancellationToken.None);
Console.WriteLine("Message received from {0}: {1}", receivedMessage.From, receivedMessage.Content);
    
// Send a notification
var notification = new Notification()
{
    Id = receivedMessage.Id,
    Event = Event.Received
};
    
await clientChannel.SendNotificationAsync(notification);
    
// Receive a notification
var receivedNotification = await clientChannel.ReceiveNotificationAsync(CancellationToken.None);
Console.WriteLine("Notification received from {0}: {1}", receivedNotification.From, receivedNotification.Event);

```

### Commands

```csharp

// Setting the presence
var presence = new Presence()
{
    Status = PresenceStatus.Available
};

try
{
    await clientChannel.SetResourceAsync(
        new LimeUri(UriTemplates.PRESENCE), 
        presence, 
        CancellationToken.None);
}
catch (LimeException ex)
{
    Console.WriteLine("Command failed - Reason: {0}", ex.Reason);
}

// Send other commands
var getContactsCommand = new Command()
{
    Method = CommandMethod.Get,
    Uri = new LimeUri(UriTemplates.CONTACTS)
};

// You can use the ProcessCommandMethodAsync instead
await clientChannel.SendCommandAsync(getContactsCommand);
var getContactsCommandResponse = await clientChannel.ReceiveCommandAsync(CancellationToken.None);

if (getContactsCommandResponse.Id == getContactsCommand.Id &&
    getContactsCommandResponse.Status == CommandStatus.Success)
{
    var contacts = getContactsCommandResponse.Resource as DocumentCollection;

    foreach (Contact item in contacts)
    {
        // ...
    }
}

```

# Server

### Receiving connections and creating the channel


```csharp

// Create and start a listener
var listenerUri = new Uri("net.tcp://localhost:55321");
X509Certificate2 serverCertificate = null;  // Mandatory for TLS
var serializer = new EnvelopeSerializer();  // Default serializer

var tcpTransportListener = new TcpTransportListener(
    listenerUri,
    serverCertificate,
    serializer);

await tcpTransportListener.StartAsync();
            
// Accept new transport connection           
var transport = await tcpTransportListener.AcceptTransportAsync(CancellationToken.None);

// Creates a new server channel
var sessionId = Guid.NewGuid();
var serverNode = Node.Parse("server@domain.com/default");
var sendTimeout = TimeSpan.FromSeconds(60);

var serverChannel = new ServerChannel(
    sessionId,
    serverNode,
    transport,
    sendTimeout);

```

### Session establishment


```csharp

// Awaits for the 'new' session envelope from the client
var receivedSession = await serverChannel.ReceiveNewSessionAsync(CancellationToken.None);

// Send the negotiation options to the client and receives the client choices
var compressionOptions = transport.GetSupportedCompression();
var encryptionOptions = transport.GetSupportedEncryption();

receivedSession = await serverChannel.NegotiateSessionAsync(
    compressionOptions,
    encryptionOptions,
    CancellationToken.None);

// Send the options confirmation
await serverChannel.SendNegotiatingSessionAsync(
    receivedSession.Compression.Value,
    receivedSession.Encryption.Value);

// Apply the options to the transport
await transport.SetCompressionAsync(
    receivedSession.Compression.Value, 
    CancellationToken.None);
await transport.SetEncryptionAsync(
    receivedSession.Encryption.Value, 
    CancellationToken.None);

// Send the authentication schemes options and receive the authentication
var schemeOptions = new AuthenticationScheme[] { AuthenticationScheme.Plain };
receivedSession = await serverChannel.AuthenticateSessionAsync(schemeOptions, CancellationToken.None);

if (receivedSession.Authentication is PlainAuthentication &&
    ((PlainAuthentication)receivedSession.Authentication).GetFromBase64Password().Equals("mypassword"))
{
    await serverChannel.SendEstablishedSessionAsync(Node.Parse("client@domain.com"));
}

```

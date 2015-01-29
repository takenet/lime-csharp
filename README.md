LIME - A lightweight messaging library
================================

LIME allows you to build scalable, real-time messaging applications using a JSON-based [open protocol](http://limeprotocol.org). It's **fully asynchronous** and supports any persistent transport like TCP or Websockets.

You can send and receive any type of object into the wire as long it can be represented as JSON or text (plain or encoded with base64) and it has a **MIME type** to allow the other party handle it in the right way. 

The connected nodes can send receipts to the other parties to notify events about messages (for instance, a message was received or the content invalid or not supported).

Besides that there's a **REST capable** command interface with verbs (*get, set and delete*) and resource identifiers (URIs) to allow rich messaging scenarios. You can use that to provide services like on-band account registration or instance-messaging resources, like presence or roster management.

Finally it has built-in support for authentication, transport encryption and compression.

### Nuget packages

- **Lime.Protocol**: Base protocol
- **Lime.Transport.Tcp**: TCP transport implementation
- **Lime.Transport.Http**: HTTP emulation layer (Lime over HTTP)
- **Lime.Messaging**: Common content and resource types for instant messaging applications

Protocol overview
---------------------

The basic protocol data package is called **envelope**. As mentioned before there are four types:

* **Message** - Transports content between nodes
* **Notification** - Notify about message events
* **Command** - Provides an interface for resource management
* **Session** - Establishes the communication channel

All envelope types share some properties (like the ```id``` - the envelope unique identifier - and the ```from``` and ```to``` routing information) but there are some unique properties of each one that allows the proper deserialization when a JSON object is received by the transport.

The ```ITransport``` interface represents a persistent transport connection that allows the management of the connection state besides sending and receiving envelopes. Currently, the library only provides the ```TcpTransport``` implementation but a ```WebSocketTransport``` is on the way.

A transport instance need an ```ISerializer``` to transform envelope objects into JSON strings. The library has a built-in serializer but it has partial support for the **Newtonsoft.Json** library (there is some limitations about some content types). We had support for the ServiceStack.Text library too, but we took it off due to licensing issues.

When two nodes are connected to each other a session can be established between they. To help the management of the session state, the library defines the ```IChannel``` interface, an abstraction of the session over the ```ITransport``` instance. The node that received the connection is the **server** and the one who is connecting is the **client**. There is specific implementations of the interface for the server (```ServerChannel``` that implements the derived ```IServerChannel``` interface) and the client (```ClientChannel``` that implements ```IClientChannel```), each one providing specific functionality for each role in the connection. The only difference between the client and the server is related to the session state management, where the server has full control of it. Besides that, they share the same set of funcionality. 

A server uses an ```ITransportListener``` instance to listen for new transport connections. The library provides the ```TcpTransportListener``` for TCP socket transport connections.

### Starting a connection

For starting a connection with a server in a specific domain, the client can use **DNS queries** to check for a *lime* SRV entry and get the server address. This is not mandatory and the client can use static connection information, but its a good idea to rely on DNS since the protocol is domain based. To open the connection the method ```OpenAsync``` of ```ITransport``` should be called passing the remote URI (in the server, the URI parameter can be null). 

After connecting the transport the client should send a **new session** envelope to starts the session negotiation. The ```IClientChannel``` interface provides the method ```StartNewSessionAsync``` for that.

#### Examples
##### Creating a client channel
```csharp
// Creates a new transport and connect to the server
var serverUri = new Uri("net.tcp://localhost:55321");

var transport = new TcpTransport();
await transport.OpenAsync(serverUri, CancellationToken.None);

// Creates a new client channel
var sendTimeout = TimeSpan.FromSeconds(60);

var clientChannel = new ClientChannel(
    transport,
    sendTimeout);

```

##### Receiving a connection and creating a server channel
```csharp
// Create and start a listener
var listenerUri = new Uri("net.tcp://localhost:55321");
X509Certificate2 serverCertificate = null;  // You should provide a value for TLS
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

The server is the responsible for establishment of the session and its parameters, like the ```id``` and node information. It can optionally negotiate transport options and authenticate the client using a supported scheme. To start the establishment process, the server calls the ```ReceiveNewSessionAsync``` method. Note that the protocol did not dictate that the session negotiation and authentication are mandatory. In fact, after receiving a **new session** envelope, the server can just send an **established session** envelope to the client to start the envelope exchanging.

During the transport options negotiation, the server sends to the client the available compression and encryption options and allows it to choose which one it wants to use in the session. This is done through the ```NegotiateSessionAsync``` method which allows the server to await for the client choices. The client select its options using the ```NegotiateSessionAsync``` method. After receiving and validating the client choices the server echoes they to the client to allow it to apply the transport options and does itself the same. The ```ITransport``` interface has the methods ```SetCompressionAsync``` and ```SetEncryptionAsync``` for this reason, but the ```ChannelBase``` implementation already does that automatically.

The most relevant transport option is the encryption. The library support **TLS encryption** for the ```TcpTransport``` implementation, allowing both server and client authentication via certificates.

After the transport options negotiation, the server can request client authentication, calling the ```AuthenticateSessionAsync``` method. The server presents to the client the available schemes and the client A node identify itself with an identity, which is presented as **name@domain** (like an e-mail). Usually the domain is the same of the server if the client is using a local authentication scheme (username/password) but can be a stranger domain if the client is using transport authentication (TLS certificate). 

When the server establishes the session, it assign to the client an unique node identifier, in the format **name@domain/instance** similar to the Jabber ID in the XMPP protocol. This identifier is important for envelope routing in multi-party server connection scenarios. 

####Examples
#####Client negotiation

```csharp
// Method 1: Establish the session using a helper method
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

// Method 2: Establish the session manually
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
    // The session is established
}

```

#####Server negotiation
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

### Exchanging envelopes

With an established session the nodes can exchange messages, notifications and commands until the server finishes the session. The ```IChannel``` interface defines methods to send and receive specific envelopes, like the ```SendMessageAsync``` and ```ReceiveMessageAsync``` for messages.

####Routing

The protocol doesn't defines explicitly how envelope routing should work during a session. The only thing defined is that if an originator does not provide the ```to``` property value, it means that the message is addressed to the immediate remote party; in the same way if a node has received an envelope without the ```from``` property value, it must assume that the envelope is originated by the remote party. 

An originator can send an envelope addresses to any destination to the other party and it may or may not accept it. But an originator should address an envelope to an node different of the remote party only if it trust it for receiving these envelopes. A remote party can be trusted for that if it has presented a valid domain certificate during the session negotiation. In this case, this node can receive and send envelopes for any identity of the authenticated domain.

#### Examples
##### Messages and notifications
```csharp
// Sending a plain text message to the remote party
var textMessage = new Message()
{
    Content = new PlainText()
    {
        Text = "Hello!"
    }
};
await clientChannel.SendMessageAsync(textMessage);

// Sending a typed JSON message
var chatStateMessage = new Message()
{
    Content = new ChatState()               // The MIME type is application/vnd.lime.chatstate+json
    {                                       // defined by the IDocument interface implementation
        State = ChatStateEvent.Composing    
    }
};    
await clientChannel.SendMessageAsync(chatStateMessage);


// Sending a generic JSON message addressed to a specific node
var jsonMessage = new Message()
{
    To = Node.Parse("anyone@domain.com"),
    Content = new JsonDocument(MediaType.Parse("application/json")) 
    {
        { "property1", "string value" },
        { "property2", 2 },
        { "property3", true },

    }
};
await clientChannel.SendMessageAsync(jsonMessage);

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

##### Commands

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

// You can use the ProcessCommandMethodAsync helper method instead
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

### Closing the session

The server is responsible for closing the session and it can do it any time by sending a **finished session envelope** to the client, but the client can ask the server to finish it simply by sending a **finishing session envelope**. 

The server should close the transport after sending the finished or failed session envelope and the client after receiving any session envelope after the session was established. The ```ClientChannel``` and ```ServerChannel``` classes already closes the transport in these cases.

#### Examples
##### Closing by the client side

``` csharp
// Do the session negotiation and establishment
// ...

if (session.State == SessionState.Established)
{
    // After the establishment of the session, the client should always  
    // await for a session envelope from the server
    var receivedSessionTask = clientChannel
        .ReceiveFinishedSessionAsync(CancellationToken.None)
        .ContinueWith(t => 
        { 
            // Do something when the server closes the session
            if (t.Result.State == SessionState.Finished)
            {
                // Session gracefully finished
            }
            else
            {
                // Finished with error
            }
        });
    
    // Consumes the channel
    // ...
    
    // Requesting the session finishing
    await clientChannel.SendFinishingSessionAsync();
    
    await receivedSesionTask;
}

```

##### Closing by the server side

``` csharp
// Do the session negotiation and establishment
// ...

// Establishes the session
await serverChannel.SendEstablishedSessionAsync(Node.Parse("client@domain.com"));

// Await for the finishing session envelope from the client
// and sends a finished session envelope when it is received
var receivedSessionEnvelopeTask = serverChannel
    .ReceiveFinishingSessionAsync(CancellationToken.None)
    .ContinueWith(t => serverChannel.SendFinishedSessionAsync());    

// Consumes the channel
// ...
```


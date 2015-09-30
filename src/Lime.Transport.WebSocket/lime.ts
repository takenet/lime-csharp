interface IEnvelope {
    id?: string;
    from?: string;
    to?: string;
    pp?: string;
    metadata?: any;
}

interface ISession extends IEnvelope {
    state: string;
    encryptionOptions?: string[];
    encryption?: string;
    compressionOptions?: string[];
    compression?: string;
    scheme?: string;
    authentication?: any;
    reason?: IReason;    
}

class SessionState {
    static new = "new";
    static negotiating = "negotiating";
    static authenticating = "authenticating";
    static established = "established";
    static finishing = "finishing";
    static finished = "finished";
    static failed = "failed";
}

class SessionEncryption {
    static none = "none";
    static tls = "tls";
}

class SessionCompression {
    static none = "none";
    static gzip = "gzip";
}

class AuthenticationScheme {
    static guest = "guest";
    static plain = "plain";
    static transport = "transport";
    static key = "key";
}

interface IAuthentication { }
class GuestAuthentication implements IAuthentication { }
class TransportAuthentication implements IAuthentication { }
class PlainAuthentication implements IAuthentication {
    password: string;
}
class KeyAuthentication implements IAuthentication {
    key: string;
}

interface IReason {
    code: number;
    description?: string;
}

interface IMessage extends IEnvelope {
    type: string;
    content: any;
}

interface INotification extends IEnvelope {
    event: string;
    reason?: IReason;
}

class NotificationEvent {
    static accepted = "accepted";
    static validated = "validated";
    static authorized = "authorized";
    static dispatched = "dispatched";
    static received = "received";
    static consumed = "consumed";
}

interface ICommand extends IEnvelope {
    uri?: string;
    type?: string;
    resource?: any;
    method: string;
    status?: string;
    reason?: IReason;
}

class CommandMethod {
    static get = "get";
    static set = "set";
    static delete = "delete";
    static observe = "observe";
    static subscribe = "subscribe";
}

class CommandStatus {
    static success = "success";
    static failure = "failure";
}

interface ITransport {
    send(envelope: IEnvelope): void;    
    onEnvelope: (envelope: IEnvelope) => any;    
    open(uri: string): void;
    close();
    stateListener: ITransportStateListener;
    getSupportedCompression(): string[];
    compression: string;
    setCompression(compression: string): void;
    getSupportedEncryption(): string[];
    encryption: string;
    setEncryption(encryption: string): void;    
}

interface ITransportEnvelopeListener {
    (envelope: IEnvelope);
}

interface ITransportStateListener {
    onOpen: () => void;
    onClosed: () => void;
    onError: (exception: string) => void;
}

class WebSocketTransport implements ITransport {
    private traceEnabled: boolean;
    webSocket: WebSocket;

    constructor(traceEnabled: boolean = false) {
        this.traceEnabled = traceEnabled;
    }


    send(envelope: IEnvelope) {
        this.ensureSocketOpen();
        const envelopeString = JSON.stringify(envelope);
        this.webSocket.send(
            envelopeString);

        if (this.traceEnabled) {
            console.debug(`SEND: ${envelopeString}`);
        }
    }
    
    onEnvelope(envelope: IEnvelope) { }
    
    open(uri: string) {
        this.webSocket = new WebSocket(uri, "lime");

        if (uri.indexOf("wss://") > -1) {
            this.encryption = SessionEncryption.tls;
        } else {
            this.encryption = SessionEncryption.none;            
        }

        this.compression = SessionCompression.none;

        this.webSocket.onmessage = e => {
            if (this.traceEnabled) {
                console.debug(`RECEIVE: ${e.data}`);
            }

            const object = JSON.parse(e.data);
            let envelope: IEnvelope;
            if (object.hasOwnProperty("event")) {
                envelope = <INotification>object;
            } else if (object.hasOwnProperty("content")) {
                envelope = <IMessage>object;
            } else if (object.hasOwnProperty("method")) {
                envelope = <ICommand>object;
            } else if (object.hasOwnProperty("state")) {
                envelope = <ISession>object;
            } else {
                return;
            }
            this.onEnvelope(envelope);            
        }

        this.webSocket.onopen = e => {            
            if (this.stateListener != null) {
                this.stateListener.onOpen();
            }
        }
        this.webSocket.onclose = e => {
            if (this.stateListener != null) {
                this.stateListener.onClosed();
            }
            this.webSocket = null;
        }
        this.webSocket.onerror = e => {
            if (this.stateListener != null) {
                this.stateListener.onError(e.toString());
            }
            this.webSocket = null;
            console.log(e);            
        }
    }

    close() {
        this.ensureSocketOpen();
        this.webSocket.close();
    }

    stateListener: ITransportStateListener;

    private ensureSocketOpen() {
        if (this.webSocket == null ||
            this.webSocket.readyState !== WebSocket.OPEN) {
            throw "The connection is not open";
        }
    }

    getSupportedCompression(): string[] { throw new Error("Encryption change is not supported"); }

    setCompression(compression: string): void {}

    getSupportedEncryption(): string[] { throw new Error("Encryption change is not supported"); }

    setEncryption(encryption: string): void {}

    compression: string;

    encryption: string;
}

interface IChannel extends IMessageChannel, ICommandChannel, INotificationChannel, ISessionChannel {
    transport: ITransport;
    remoteNode: string;
    localNode: string;
    sessionId: string;
    state: string;
}

interface IMessageChannel {
    sendMessage(message: IMessage): void;
    onMessage: (message: IMessage) => void;
}

interface ICommandChannel {
    sendCommand(command: ICommand): void;
    onCommand: (command: ICommand) => void;
}

interface INotificationChannel {
    sendNotification(notification: INotification);
    onNotification: (notification: INotification) => void;
}

interface ISessionChannel {
    sendSession(session: ISession): void;
    onSession: ISessionListener;
}

interface ISessionListener {
    (session: ISession): void;
}

class Channel implements IChannel {
    private autoReplyPings: boolean;
    private autoNotifyReceipt: boolean;

    constructor(transport: ITransport, autoReplyPings: boolean, autoNotifyReceipt: boolean) {
        this.autoReplyPings = autoReplyPings;
        this.autoNotifyReceipt = autoNotifyReceipt;
        this.transport = transport;
        this.transport.onEnvelope = e => {            
            if (e.hasOwnProperty("event")) {
                this.onNotification(<INotification>e);
            } else if (e.hasOwnProperty("content")) {
                const message = <IMessage>e;
                if (this.autoNotifyReceipt && 
                    message.id &&
                    message.from) {
                    const notification: INotification = {
                        id: message.id,
                        to: message.from,
                        event: NotificationEvent.received
                    };
                    this.sendNotification(notification);
                }
                this.onMessage(message);
            } else if (e.hasOwnProperty("method")) {
                const command = <ICommand>e;
                if (this.autoReplyPings && 
                    command.id &&
                    command.from &&
                    command.uri === "/ping" &&                    
                    command.method === CommandMethod.get) {
                    const pingCommandResponse: ICommand = {
                        id: command.id,
                        to: command.from,
                        method: CommandMethod.get,
                        status: CommandStatus.success,
                        type: "application/vnd.lime.ping+json"
                    }
                    this.sendCommand(pingCommandResponse);                
                } else {
                    this.onCommand(command);
                }                
            } else if (e.hasOwnProperty("state")) {
                this.onSession(<ISession>e);
            }                
        };
        this.state = SessionState.new;
    }
    sendMessage(message: IMessage) {
        if (this.state !== SessionState.established) {
            throw new Error(`Cannot send in the '${this.state}' state`);
        }
        this.send(message);
    }

    onMessage(message: IMessage) { }

    sendCommand(command: ICommand) {
        if (this.state !== SessionState.established) {
            throw new Error(`Cannot send in the '${this.state}' state`);
        }
        this.send(command);
    }

    onCommand(command: ICommand) { }

    sendNotification(notification: INotification) {
        if (this.state !== SessionState.established) {
            throw new Error(`Cannot send in the '${this.state}' state`);
        }
        this.send(notification);
    }

    onNotification(notification: INotification) { }

    sendSession(session: ISession) {
        if (this.state === SessionState.finished ||
            this.state === SessionState.failed) {
            throw new Error(`Cannot send in the '${this.state}' state`);
        }

        this.send(session);
    }

    onSession(session: ISession) { }

    transport: ITransport;
    remoteNode: string;
    localNode: string;
    sessionId: string;
    state: string;

    private send(envelope: IEnvelope) {
        this.transport.send(envelope);
    }
}

interface IClientChannel extends IChannel {
    startNewSession(): void;
    negotiateSession(sessionCompression: string, sessionEncryption: string): void;
    authenticateSession(identity: string, authentication: IAuthentication, instance: string): void;
    sendFinishingSession(): void;
    onSessionNegotiating: ISessionListener;
    onSessionAuthenticating: ISessionListener;
    onSessionEstablished: ISessionListener;
    onSessionFinished: ISessionListener;
    onSessionFailed: ISessionListener;    
}

class ClientChannel extends Channel implements IClientChannel {
    constructor(transport: ITransport, autoReplyPings: boolean = true, autoNotifyReceipt: boolean = false) {
        super(transport, autoReplyPings, autoNotifyReceipt);
        super.onSession = s => {
            this.sessionId = s.id;
            this.state = s.state;

            if (s.state === SessionState.established) {
                this.localNode = s.to;
                this.remoteNode = s.from;
            } else if (s.state === SessionState.finished || s.state === SessionState.failed) {
                try {
                    this.transport.close();
                } catch (e) {
                    console.error(e);
                }
            }

            switch (s.state) {
                case SessionState.negotiating:
                    if (this.onSessionNegotiating != null) {
                        this.onSessionNegotiating(s);
                    }                    
                    break;
                case SessionState.authenticating:
                    if (this.onSessionAuthenticating != null) {
                        this.onSessionAuthenticating(s);
                    }
                    break;
                case SessionState.established:
                    if (this.onSessionEstablished != null) {
                        this.onSessionEstablished(s);
                    }
                    break;
                case SessionState.finished:
                    if (this.onSessionFinished != null) {
                        this.onSessionFinished(s);
                    }
                case SessionState.failed:
                    if (this.onSessionFailed != null) {
                        this.onSessionFailed(s);
                    }
                default:
            }                       
        }
    }

    startNewSession() {
        if (this.state !== SessionState.new) {
            throw `Cannot start a session in the '${this.state}' state.`;
        }

        const session: ISession = {
            state: SessionState.new
        };
        this.sendSession(session);
    }

    negotiateSession(sessionCompression: string, sessionEncryption: string) {
        if (this.state !== SessionState.negotiating) {
            throw `Cannot negotiate a session in the '${this.state}' state.`;
        }
        const session: ISession = {
            id: this.sessionId,
            state: SessionState.negotiating,
            compression: sessionCompression,
            encryption: sessionEncryption
        };
        this.sendSession(session);
    }

    authenticateSession(identity: string, authentication: IAuthentication, instance: string) {
        if (this.state !== SessionState.authenticating) {
            throw `Cannot authenticate a session in the '${this.state}' state.`;
        }

        let scheme: string;
        if (authentication instanceof GuestAuthentication) {
            scheme = AuthenticationScheme.guest;
        } else if (authentication instanceof PlainAuthentication) {
            scheme = AuthenticationScheme.plain;
        } else if (authentication instanceof TransportAuthentication) {
            scheme = AuthenticationScheme.transport;
        } else if (authentication instanceof KeyAuthentication) {
            scheme = AuthenticationScheme.key;
        } else {
            scheme = "unknown";
        }

        const session: ISession = {
            id: this.sessionId,
            state: SessionState.authenticating,
            from: `${identity}/${instance}`,
            scheme: scheme,
            authentication: authentication
        };
        this.sendSession(session);
    }
    
    sendFinishingSession() {
        if (this.state !== SessionState.established) {
            throw `Cannot finish a session in the '${this.state}' state.`;
        }

        const session: ISession = {
            id: this.sessionId,
            state: SessionState.finishing
        };
        this.sendSession(session);        
    }

    onSessionNegotiating(session: ISession) {}

    onSessionAuthenticating(session: ISession) {}

    onSessionEstablished(session: ISession) {}

    onSessionFinished(session: ISession) {}

    onSessionFailed(session: ISession) {}
}

class ClientChannelExtensions {
    static establishSession(clientChannel: IClientChannel, compression: string, encryption: string, identity: string, authentication: any, instance: string, listener: IEstablishSessionListener): void {
        if (clientChannel.state !== SessionState.new) {
            throw `Cannot establish a session in the '${clientChannel.state}' state.`;
        }
        
        clientChannel.onSessionNegotiating = s => {
            try {

                if (s.encryptionOptions != null || s.compressionOptions != null) {
                    let sessionCompression = compression;
                    if (sessionCompression === null) {
                        sessionCompression = s.compressionOptions[0];
                    }

                    let sessionEncryption = encryption;
                    if (sessionEncryption === null) {
                        sessionEncryption = s.encryptionOptions[0];
                    }

                    clientChannel.negotiateSession(sessionCompression, sessionEncryption);

                } else {
                    // Apply transport options
                    if (s.compression !== clientChannel.transport.compression) {
                        clientChannel.transport.setCompression(s.compression);
                    }
                    
                    if (s.encryption !== clientChannel.transport.encryption) {
                        clientChannel.transport.setEncryption(s.encryption);
                    }
                }
            } catch (e1) {
                this.onFailure(clientChannel, listener, e1);
            }            
        }

        clientChannel.onSessionAuthenticating = s => {
            try {
                clientChannel.authenticateSession(identity, authentication, instance);
            } catch (e2) {
                this.onFailure(clientChannel, listener, e2);
            }
        }

        clientChannel.onSessionEstablished = s => {
            this.onResult(clientChannel, listener, s);
        }

        clientChannel.onSessionFailed = s => {
            this.onResult(clientChannel, listener, s);
        }

        try {
            clientChannel.startNewSession();
        } catch (e) {
            this.onFailure(clientChannel, listener, e);
        }
    }

    private static onResult(clientChannel: IClientChannel, listener: IEstablishSessionListener, session: ISession): void {
        this.removeListeners(clientChannel);
        listener.onResult(session);
    }

    private static onFailure(clientChannel: IClientChannel, listener: IEstablishSessionListener, exception: string): void {
        this.removeListeners(clientChannel);
        listener.onFailure(exception);        
    }

    private static removeListeners(clientChannel: IClientChannel): void {
        clientChannel.onSessionNegotiating = null;
        clientChannel.onSessionAuthenticating = null;
        clientChannel.onSessionEstablished = null;
        clientChannel.onSessionFailed = null;
    }
}

interface IEstablishSessionListener {
    onResult: ISessionListener;
    onFailure: (exception: string) => void;
}

function newGuid(): string {
    var d = new Date().getTime();
    const uuid = "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, c => {
        var r = (d + Math.random() * 16) % 16 | 0;
        d = Math.floor(d / 16);
        return (c === "x" ? r : (r & 0x3 | 0x8)).toString(16);
    });
    return uuid;
}

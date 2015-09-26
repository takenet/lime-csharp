interface IEnvelope {
    id?: string;
    from?: string;
    to?: string;
    pp?: string;
    metadata?: any;
}

class SessionState {
    static new              = "new";
    static negotiating      = "negotiating";
    static authenticating   = "authenticating";
    static established      = "established";
    static finishing        = "finishing";
    static finished         = "finished";        
    static failed           = "failed";        
}

interface ISession extends IEnvelope {
    state: string;
}

interface IMessage extends IEnvelope {
    type: string;
    content: any;
}

class NotificationEvent {
    static accepted     = "accepted";
    static validated    = "validated";
    static authorized   = "authorized";
    static dispatched   = "dispatched";
    static received     = "received";
    static consumed     = "consumed";
}

interface IReason {
    code: number;
    description?: string;
}

interface INotification extends IEnvelope {
    event: string;
    reason?: IReason;
}

class CommandMethod {
    static get          = "get";
    static set          = "set";
    static delete       = "delete";
    static observe      = "observe";
    static subscribe    = "subscribe";
}

class CommandStatus {
    static success = "success";
    static failure = "failure";
}

interface ICommand extends IEnvelope {
    uri?: string;
    type?: string;
    resource?: any;
    method: string;
    status: string;
    reason?: IReason;
}

interface ITransport {
    send(envelope: IEnvelope);    
    onEnvelope: (envelope: IEnvelope) => any;
    setStateListener(listener: ITransportStateListener);       
    open(uri: string);
    close();
}

interface ITransportEnvelopeListener {
    (envelope: IEnvelope);
}

interface ITransportStateListener {
    onOpen();
    onClosed();
    onError();
}

class WebSocketTransport implements ITransport {

    private webSocket: WebSocket;
    private transportStateListener: ITransportStateListener;

    send(envelope: IEnvelope) {
        this.ensureSocketOpen();
        this.webSocket.send(
            JSON.stringify(envelope));
    }
    
    onEnvelope(envelope: IEnvelope) { }

    setStateListener(listener: ITransportStateListener) {
        this.transportStateListener = listener;
    }

    open(uri: string) {
        this.webSocket = new WebSocket(uri, "lime");
        this.webSocket.onmessage = e => {            
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
            if (this.transportStateListener != null) {
                this.transportStateListener.onOpen();
            }
        }
        this.webSocket.onclose = e => {
            if (this.transportStateListener != null) {
                this.transportStateListener.onClosed();
            }
            this.webSocket = null;
        }
        this.webSocket.onerror = e => {
            if (this.transportStateListener != null) {
                this.transportStateListener.onError();
            }
            this.webSocket = null;
            console.log(e);            
        }
    }

    close() {
        this.ensureSocketOpen();
        this.webSocket.close();
    }

    private ensureSocketOpen() {
        if (this.webSocket == null ||
            this.webSocket.readyState !== WebSocket.OPEN) {
            throw "The connection is not open";
        }
    }    
}

interface IChannel extends IMessageChannel, ICommandChannel, INotificationChannel, ISessionChannel {
    transport: ITransport;
    remoteNode: string;
    localNode: string;
    sessionId: string;
    state: string;
}

interface IMessageChannel {
    sendMessage(message: IMessage);
    onMessage: (message: IMessage) => any;
}

interface ICommandChannel {
    sendCommand(command: ICommand);
    onCommand: (command: ICommand) => any;
}

interface INotificationChannel {
    sendNotification(notification: INotification);
    onNotification: (notification: INotification) => any;
}

interface ISessionChannel {
    sendSession(session: ISession);
    onSession: ISessionListener;
}


interface ISessionListener {
    (session: ISession);
}

class Channel implements IChannel {

    constructor(transport: ITransport) {
        this.transport = transport;
        this.transport.onEnvelope = e => {
            if (e.hasOwnProperty("event")) {
                this.onNotification(<INotification>e);
            } else if (e.hasOwnProperty("content")) {
                this.onMessage(<IMessage>e);                
            } else if (e.hasOwnProperty("method")) {
                this.onCommand(<ICommand>e);
            } else if (e.hasOwnProperty("state")) {
                this.onSession(<ISession>e);
            }                
        };
        this.state = SessionState.new;
    }

    sendMessage(message: IMessage) {
        if (this.state !== SessionState.established) {
            throw `Cannot send in the '${this.state}' state`;
        }
        this.send(message);
    }

    onMessage(message: IMessage) { }

    sendCommand(command: ICommand) {
        if (this.state !== SessionState.established) {
            throw `Cannot send in the '${this.state}' state`;
        }
        this.send(command);
    }

    onCommand(command: ICommand) { }

    sendNotification(notification: INotification) {
        if (this.state !== SessionState.established) {
            throw `Cannot send in the '${this.state}' state`;
        }
        this.send(notification);
    }

    onNotification(notification: INotification) { }

    sendSession(session: ISession) {
        if (this.state === SessionState.finished ||
            this.state === SessionState.failed) {
            throw `Cannot send in the '${this.state}' state`;
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
    startNewSession(sessionListener: ISessionListener);
}

class ClientChannel extends Channel implements IClientChannel {
    constructor(transport: ITransport) {
        super(transport);
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
        }
    }

    startNewSession(sessionListener: ISessionListener) {
        
    }
}

function establishSession() {
    var webSocket = new WebSocket("ws://localhost:8080", "lime");

    webSocket.onmessage = event => {
        console.log(event.data);
    };

    var session: ISession = {
        state: SessionState.new
    }    

    webSocket.onopen = event => {
        webSocket.send(
            JSON.stringify(session));
    }
}
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
    setListener(listener: ITransportListener);       
    open(url: string);
    close();
}

interface ITransportListener {
    onEnvelopeReceived(envelope: IEnvelope);
}

class WebSocketTransport implements ITransport {

    private webSocket: WebSocket;
    private listener: ITransportListener;
    private queue: IEnvelope[];

    constructor() {
        this.queue = [];
    }

    send(envelope: IEnvelope) {
        if (this.webSocket == null) {
            throw "The connection is not open";
        }
        if (this.webSocket.readyState === WebSocket.OPEN) {
            this.webSocket.send(
                JSON.stringify(envelope));
        }

        this.queue.push(envelope);
    }
    
    setListener(listener: ITransportListener) {
        this.listener = listener;
    }

    open(url: string) {
        this.webSocket = new WebSocket(url, "lime");
        this.webSocket.onmessage = e => {
            if (this.listener != null) {
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
                this.listener.onEnvelopeReceived(envelope);
            }
        }

        this.webSocket.onopen = (e): void => {
            while (this.queue.length > 0) {
                this.send(this.queue.pop());
            }
        }
    }

    close() {
        this.webSocket.close();
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
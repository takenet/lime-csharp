interface IEnvelope {
    id?: string;
    from?: string;
    to?: string;
    pp?: string;
    metadata?: any;
}

class SessionState {
    static new = "new";
    static negotiating = "negotiating";
    static authenticating = "authenticating";
    static established = "established";
    static finishing = "finishing";
    static finished = "finished";        
}

interface ISession extends IEnvelope {
    state: string;
}

interface IMessage extends IEnvelope {
    type: string;
    content: any;
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

class WebSocketReadyState {
    static CONNECTING = 0;
    static OPEN = 1;
    static CLOSING = 2;
    static CLOSED = 3;
}

class WebSocketTransport implements ITransport {

    private webSocket: WebSocket;
    private listener: ITransportListener;

    send(envelope: IEnvelope) {
        this.webSocket.send(
            JSON.stringify(envelope));        
    }
    
    setListener(listener: ITransportListener) {
        this.listener = listener;
    }

    open(url: string) {
        this.webSocket = new WebSocket(url, "lime");
        this.webSocket.onopen = e => {      
            this.webSocket.onmessage = ev => {
                
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
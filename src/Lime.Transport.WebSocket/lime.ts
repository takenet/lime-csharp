interface Envelope {
    id?: String;
    from?: String;
    to?: String;
    pp?: String;
    metadata?: any;
}

enum SessionState {
    new,
    negotiating,
    authenticating,
    established,
    finishing,
    finished
}

interface Session extends Envelope {
    state: SessionState;
}

interface Message extends Envelope {
    type: String;
    content: any;
}


function establishSession() {
    var webSocket = new WebSocket("ws://localhost", "lime");

    webSocket.onmessage = event => {
        console.log(event.data);
    };

    var session: Session = {
        state: SessionState.new
    }

    webSocket.onopen = event => {
        webSocket.send(
            JSON.stringify(session));
    }
} 
 
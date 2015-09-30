var clientChannel: IClientChannel;


var connectButton = <HTMLButtonElement>document.getElementById("connect-button");
connectButton.onclick = e => {
    var identityInput = <HTMLInputElement>document.getElementById("identity-input");
    var instanceInput = <HTMLInputElement>document.getElementById("instance-input");
    var uriInput = <HTMLInputElement>document.getElementById("uri-input");

    if (!identityInput.value) {
        alert("The identity value is mandatory");
        return;
    }

    if (!instanceInput.value) {
        alert("The instance value is mandatory");
        return;
    }

    if (!uriInput.value) {
        alert("The URI value is mandatory");
        return;
    }

    establishSession(uriInput.value, identityInput.value, instanceInput.value);
};
connectButton.disabled = false;

var disconnectButton = <HTMLButtonElement>document.getElementById("disconnect-button");
disconnectButton.onclick = e => {
    if (isSessionEstablished()) {
        clientChannel.sendFinishingSession();        
    }    
}
disconnectButton.disabled = true;

var messageSendButton = <HTMLButtonElement>document.getElementById("message-send-button");
messageSendButton.onclick = e => {
    if (isSessionEstablished()) {
        var messageToInput = <HTMLInputElement>document.getElementById("message-to-input");
        var messageContentInput = <HTMLInputElement>document.getElementById("message-content-input");
        var message: IMessage = {
            to: messageToInput.value,
            type: "text/plain",
            content: messageContentInput.value
        };
        clientChannel.sendMessage(message);
    }        
}

function isSessionEstablished(): boolean {
    return clientChannel != null && clientChannel.state === SessionState.established;
}

function establishSession(uri: string, identity: string, instance: string) {
    var transport = new WebSocketTransport(true);
    transport.stateListener = {
        onOpen: () => {
            clientChannel = new ClientChannel(transport);
            ClientChannelExtensions.establishSession(
                clientChannel,
                "none",
                "none",
                identity,
                new GuestAuthentication(),
                instance,
                {
                    onResult: s => {
                        logMessage(`Session id: ${s.id} - State: ${s.state}`);
                        if (s.state === SessionState.established) {
                            connectButton.disabled = true;
                            disconnectButton.disabled = false;
                        }
                    },
                    onFailure: e => { logMessage(`An error occurred: ${e}`); }
                });

            clientChannel.onMessage = m => {
                logMessage(`Message received - From: ${m.from} - To: ${m.to} - Content: ${m.content}`);
            };

            clientChannel.onNotification = n => {
                logMessage(`Notification received - From: ${n.from} - To: ${n.to} - Event: ${n.event} - Reason: ${n.reason}`);
            };

            clientChannel.onCommand = c => {
                logMessage(`Command received - From: ${c.from} - To: ${c.to} - Method: ${c.method} - URI: ${c.uri} - Resource: ${c.resource} - Status: ${c.status} - Reason: ${c.reason}`);
            };

            var sessionListener: ISessionListener = s => {
                connectButton.disabled = false;
                disconnectButton.disabled = true;
            }

            clientChannel.onSessionFinished = sessionListener;
            clientChannel.onSessionFailed = sessionListener;

        },
        onClosed: () => {
            logMessage("Transport is closed");
        },
        onError: (s) => {
            logMessage(`Transport failed: ${s}`);
        }
    }

    transport.open(uri);
}

var logTextarea = <HTMLTextAreaElement>document.getElementById("log-textarea");
function logMessage(message: string) {
    let log = logTextarea.value;
    if (log) {
        log += "\r\n";
    }
    log += message;
    logTextarea.value = log;
}
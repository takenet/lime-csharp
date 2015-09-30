var clientChannel: IClientChannel;


var connectButton = <HTMLButtonElement>document.getElementById("connect-button");
connectButton.onclick = e => {
    var identityInput = <HTMLInputElement>document.getElementById("identity-input");
    var instanceInput = <HTMLInputElement>document.getElementById("instance-input");
    var passwordInput = <HTMLInputElement>document.getElementById("password-input");
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

    establishSession(uriInput.value, identityInput.value, instanceInput.value, passwordInput.value);
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
            id: newGuid(),
            to: messageToInput.value,
            type: "text/plain",
            content: messageContentInput.value
        };
        clientChannel.sendMessage(message);
    }
}

var setPresenceAvailableButton = <HTMLButtonElement>document.getElementById("set-presence-available-button");
setPresenceAvailableButton.onclick = e => {
    if (isSessionEstablished()) {
        var presenceCommand: ICommand = {
            id: newGuid(),
            method: CommandMethod.set,
            uri: "/presence",
            type: "application/vnd.lime.presence+json",
            resource: {
                status: "available"
            }
        }

        clientChannel.sendCommand(presenceCommand);
    }
}

var setPresenceUnavailableButton = <HTMLButtonElement>document.getElementById("set-presence-unavailable-button");
setPresenceUnavailableButton.onclick = e => {
    if (isSessionEstablished()) {
        var presenceCommand: ICommand = {
            id: newGuid(),
            method: CommandMethod.set,
            uri: "/presence",
            type: "application/vnd.lime.presence+json",
            resource: {
                status: "unavailable"
            }
        }

        clientChannel.sendCommand(presenceCommand);
    }
}

var setReceiptsButton = <HTMLButtonElement>document.getElementById("set-receipts-button");
setReceiptsButton.onclick = e => {
    if (isSessionEstablished()) {
        var presenceCommand: ICommand = {
            id: newGuid(),
            method: CommandMethod.set,
            uri: "/receipt",
            type: "application/vnd.lime.receipt+json",
            resource: {
                events: [ "accepted", "validated", "authorized", "dispatched", "received", "consumed" ]
            }
        }

        clientChannel.sendCommand(presenceCommand);
    }
}

function isSessionEstablished(): boolean {
    return clientChannel != null && clientChannel.state === SessionState.established;
}

function establishSession(uri: string, identity: string, instance: string, password: string) {
    var transport = new WebSocketTransport(true);
    transport.stateListener = {
        onOpen: () => {
            var authentication: IAuthentication;
            if (password) {
                const plainAuthentication = new PlainAuthentication();
                plainAuthentication.password = btoa(password);
                authentication = plainAuthentication;
            } else {
                authentication = new GuestAuthentication();
            }
            
            clientChannel = new ClientChannel(transport, true, true);
            ClientChannelExtensions.establishSession(
                clientChannel,
                "none",
                "none",
                identity,
                authentication,
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
        },
        onClosed: () => {
            connectButton.disabled = false;
            disconnectButton.disabled = true;
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
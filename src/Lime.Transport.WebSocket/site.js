var clientChannel;
var connectButton = document.getElementById("connect-button");
connectButton.onclick = function (e) {
    var identityInput = document.getElementById("identity-input");
    var instanceInput = document.getElementById("instance-input");
    var passwordInput = document.getElementById("password-input");
    var uriInput = document.getElementById("uri-input");
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
var disconnectButton = document.getElementById("disconnect-button");
disconnectButton.onclick = function (e) {
    if (isSessionEstablished()) {
        clientChannel.sendFinishingSession();
    }
};
disconnectButton.disabled = true;
var messageSendButton = document.getElementById("message-send-button");
messageSendButton.onclick = function (e) {
    if (isSessionEstablished()) {
        var messageToInput = document.getElementById("message-to-input");
        var messageContentInput = document.getElementById("message-content-input");
        var message = {
            id: generateGuid(),
            to: messageToInput.value,
            type: "text/plain",
            content: messageContentInput.value
        };
        clientChannel.sendMessage(message);
    }
};
var setPresenceAvailableButton = document.getElementById("set-presence-available-button");
setPresenceAvailableButton.onclick = function (e) {
    if (isSessionEstablished()) {
        var presenceCommand = {
            id: generateGuid(),
            method: CommandMethod.set,
            uri: "/presence",
            type: "application/vnd.lime.presence+json",
            resource: {
                status: "available"
            }
        };
        clientChannel.sendCommand(presenceCommand);
    }
};
var setPresenceUnavailableButton = document.getElementById("set-presence-unavailable-button");
setPresenceUnavailableButton.onclick = function (e) {
    if (isSessionEstablished()) {
        var presenceCommand = {
            id: generateGuid(),
            method: CommandMethod.set,
            uri: "/presence",
            type: "application/vnd.lime.presence+json",
            resource: {
                status: "unavailable"
            }
        };
        clientChannel.sendCommand(presenceCommand);
    }
};
var setReceiptsButton = document.getElementById("set-receipts-button");
setReceiptsButton.onclick = function (e) {
    if (isSessionEstablished()) {
        var presenceCommand = {
            id: generateGuid(),
            method: CommandMethod.set,
            uri: "/receipt",
            type: "application/vnd.lime.receipt+json",
            resource: {
                events: ["accepted", "validated", "authorized", "dispatched", "received", "consumed"]
            }
        };
        clientChannel.sendCommand(presenceCommand);
    }
};
function isSessionEstablished() {
    return clientChannel != null && clientChannel.state === SessionState.established;
}
function establishSession(uri, identity, instance, password) {
    var transport = new WebSocketTransport(true);
    transport.stateListener = {
        onOpen: function () {
            var authentication;
            if (password) {
                var plainAuthentication = new PlainAuthentication();
                plainAuthentication.password = password;
                authentication = plainAuthentication;
            }
            else {
                authentication = new GuestAuthentication();
            }
            clientChannel = new ClientChannel(transport, true, true);
            ClientChannelExtensions.establishSession(clientChannel, "none", "none", identity, authentication, instance, {
                onResult: function (s) {
                    logMessage("Session id: " + s.id + " - State: " + s.state);
                    if (s.state === SessionState.established) {
                        connectButton.disabled = true;
                        disconnectButton.disabled = false;
                    }
                },
                onFailure: function (e) { logMessage("An error occurred: " + e); }
            });
            clientChannel.onMessage = function (m) {
                logMessage("Message received - From: " + m.from + " - To: " + m.to + " - Content: " + m.content);
            };
            clientChannel.onNotification = function (n) {
                logMessage("Notification received - From: " + n.from + " - To: " + n.to + " - Event: " + n.event + " - Reason: " + n.reason);
            };
            clientChannel.onCommand = function (c) {
                logMessage("Command received - From: " + c.from + " - To: " + c.to + " - Method: " + c.method + " - URI: " + c.uri + " - Resource: " + c.resource + " - Status: " + c.status + " - Reason: " + c.reason);
            };
        },
        onClosed: function () {
            connectButton.disabled = false;
            disconnectButton.disabled = true;
            logMessage("Transport is closed");
        },
        onError: function (s) {
            logMessage("Transport failed: " + s);
        }
    };
    transport.open(uri);
}
var logTextarea = document.getElementById("log-textarea");
function logMessage(message) {
    var log = logTextarea.value;
    if (log) {
        log += "\r\n";
    }
    log += message;
    logTextarea.value = log;
}

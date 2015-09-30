var clientChannel;
var connectButton = document.getElementById("connect-button");
connectButton.onclick = function (e) {
    var identityInput = document.getElementById("identity-input");
    var instanceInput = document.getElementById("instance-input");
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
    establishSession(uriInput.value, identityInput.value, instanceInput.value);
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
            to: messageToInput.value,
            type: "text/plain",
            content: messageContentInput.value
        };
        clientChannel.sendMessage(message);
    }
};
function isSessionEstablished() {
    return clientChannel != null && clientChannel.state === SessionState.established;
}
function establishSession(uri, identity, instance) {
    var transport = new WebSocketTransport(true);
    transport.stateListener = {
        onOpen: function () {
            clientChannel = new ClientChannel(transport);
            ClientChannelExtensions.establishSession(clientChannel, "none", "none", identity, new GuestAuthentication(), instance, {
                onResult: function (s) {
                    console.log("Session id: " + s.id + " - State: " + s.state);
                    if (s.state === SessionState.established) {
                        connectButton.disabled = true;
                        disconnectButton.disabled = false;
                    }
                },
                onFailure: function (e) { console.error("An error occurred: " + e); }
            });
            clientChannel.onMessage = function (m) {
                console.log("Message received - From: " + m.from + " - To: " + m.to + " - Content: " + m.content);
            };
            clientChannel.onNotification = function (n) {
                console.log("Notification received - From: " + n.from + " - To: " + n.to + " - Event: " + n.event + " - Reason: " + n.reason);
            };
            clientChannel.onCommand = function (c) {
                console.log("Command received - From: " + c.from + " - To: " + c.to + " - Method: " + c.method + " - URI: " + c.uri + " - Resource: " + c.resource + " - Status: " + c.status + " - Reason: " + c.reason);
            };
            var sessionListener = function (s) {
                connectButton.disabled = false;
                disconnectButton.disabled = true;
            };
            clientChannel.onSessionFinished = sessionListener;
            clientChannel.onSessionFailed = sessionListener;
        },
        onClosed: function () {
            console.log("Transport is closed");
        },
        onError: function (s) {
            console.error("Transport failed: " + s);
        }
    };
    transport.open(uri);
}

function establishSession() {
    var transport = new WebSocketTransport(true);
    transport.stateListener = {
        onOpen: function () {
            var channel = new ClientChannel(transport);
            ClientChannelExtensions.establishSession(channel, "none", "none", "any@limeprotocol.org", new GuestAuthentication(), "default", {
                onResult: function (s) { console.log("Session id: " + s.id + " - State: " + s.state); },
                onFailure: function (e) { console.error("An error occurred: " + e); }
            });
            channel.onMessage = function (m) {
                console.log("Message received - From: " + m.from + " - To: " + m.to + " - Content: " + m.content);
            };
        },
        onClosed: function () {
            console.log("Transport is closed");
        },
        onError: function (s) {
            console.error("Transport failed: " + s);
        }
    };
    transport.open("ws://localhost:8080");
}

function  establishSession() {
    var transport = new WebSocketTransport(true);

    transport.stateListener = {
        onOpen: () => {
            var channel = new ClientChannel(transport);
            ClientChannelExtensions.establishSession(
                channel,
                "none",
                "none",
                "any@limeprotocol.org",
                new GuestAuthentication(),
                "default",
                {
                    onResult: s => { console.log(`Session id: ${s.id} - State: ${s.state}`); },
                    onFailure: e => { console.error(`An error occurred: ${e}`); }
                });

            channel.onMessage = m => {
                console.log(`Message received - From: ${m.from} - To: ${m.to} - Content: ${m.content}`);
            };

        },
        onClosed: () => {
            console.log("Transport is closed");
        },
        onError: (s) => {
            console.error(`Transport failed: ${s}`);
        }
    }

    transport.open("ws://localhost:8080");
}
function establishSession() {
    var webSocket = new WebSocket("ws://localhost", "lime");

    webSocket.onmessage = function (event) {
        console.log(event.data);
    };

    webSocket.onopen = function(event) {
        webSocket.send(
            JSON.stringify({
                state: "new"
            }));
    }
}

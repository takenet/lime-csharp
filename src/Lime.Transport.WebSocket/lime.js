var SessionState;
(function (SessionState) {
    SessionState[SessionState["new"] = 0] = "new";
    SessionState[SessionState["negotiating"] = 1] = "negotiating";
    SessionState[SessionState["authenticating"] = 2] = "authenticating";
    SessionState[SessionState["established"] = 3] = "established";
    SessionState[SessionState["finishing"] = 4] = "finishing";
    SessionState[SessionState["finished"] = 5] = "finished";
})(SessionState || (SessionState = {}));
function establishSession() {
    var webSocket = new WebSocket("ws://localhost", "lime");
    webSocket.onmessage = function (event) {
        console.log(event.data);
    };
    var session = {
        state: SessionState.new
    };
    webSocket.onopen = function (event) {
        webSocket.send(JSON.stringify(session));
    };
}

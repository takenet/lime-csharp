var SessionState = (function () {
    function SessionState() {
    }
    SessionState.new = "new";
    SessionState.negotiating = "negotiating";
    SessionState.authenticating = "authenticating";
    SessionState.established = "established";
    SessionState.finishing = "finishing";
    SessionState.finished = "finished";
    return SessionState;
})();
var WebSocketReadyState = (function () {
    function WebSocketReadyState() {
    }
    WebSocketReadyState.CONNECTING = 0;
    WebSocketReadyState.OPEN = 1;
    WebSocketReadyState.CLOSING = 2;
    WebSocketReadyState.CLOSED = 3;
    return WebSocketReadyState;
})();
var WebSocketTransport = (function () {
    function WebSocketTransport() {
    }
    WebSocketTransport.prototype.send = function (envelope) {
        this.webSocket.send(JSON.stringify(envelope));
    };
    WebSocketTransport.prototype.setListener = function (listener) {
        this.listener = listener;
    };
    WebSocketTransport.prototype.open = function (url) {
        var _this = this;
        this.webSocket = new WebSocket(url, "lime");
        this.webSocket.onopen = function (e) {
            _this.webSocket.onmessage = function (ev) {
            };
        };
    };
    WebSocketTransport.prototype.close = function () {
        this.webSocket.close();
    };
    return WebSocketTransport;
})();
function establishSession() {
    var webSocket = new WebSocket("ws://localhost:8080", "lime");
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

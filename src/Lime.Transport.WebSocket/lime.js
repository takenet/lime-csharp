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
var NotificationEvent = (function () {
    function NotificationEvent() {
    }
    NotificationEvent.accepted = "accepted";
    NotificationEvent.validated = "validated";
    NotificationEvent.authorized = "authorized";
    NotificationEvent.dispatched = "dispatched";
    NotificationEvent.received = "received";
    NotificationEvent.consumed = "consumed";
    return NotificationEvent;
})();
var CommandMethod = (function () {
    function CommandMethod() {
    }
    CommandMethod.get = "get";
    CommandMethod.set = "set";
    CommandMethod.delete = "delete";
    CommandMethod.observe = "observe";
    CommandMethod.subscribe = "subscribe";
    return CommandMethod;
})();
var CommandStatus = (function () {
    function CommandStatus() {
    }
    CommandStatus.success = "success";
    CommandStatus.failure = "failure";
    return CommandStatus;
})();
var WebSocketTransport = (function () {
    function WebSocketTransport() {
        this.queue = [];
    }
    WebSocketTransport.prototype.send = function (envelope) {
        if (this.webSocket == null) {
            throw "The connection is not open";
        }
        if (this.webSocket.readyState === WebSocket.OPEN) {
            this.webSocket.send(JSON.stringify(envelope));
        }
        this.queue.push(envelope);
    };
    WebSocketTransport.prototype.setListener = function (listener) {
        this.listener = listener;
    };
    WebSocketTransport.prototype.open = function (url) {
        var _this = this;
        this.webSocket = new WebSocket(url, "lime");
        this.webSocket.onmessage = function (e) {
            if (_this.listener != null) {
                var object = JSON.parse(e.data);
                var envelope;
                if (object.hasOwnProperty("event")) {
                    envelope = object;
                }
                else if (object.hasOwnProperty("content")) {
                    envelope = object;
                }
                else if (object.hasOwnProperty("method")) {
                    envelope = object;
                }
                else if (object.hasOwnProperty("state")) {
                    envelope = object;
                }
                else {
                    return;
                }
                _this.listener.onEnvelopeReceived(envelope);
            }
        };
        this.webSocket.onopen = function (e) {
            while (_this.queue.length > 0) {
                _this.send(_this.queue.pop());
            }
        };
        this.webSocket.onclose = function (e) {
            _this.webSocket = null;
        };
        this.webSocket.onerror = function (e) {
            console.log(e);
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

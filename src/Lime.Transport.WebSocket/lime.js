var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
var SessionState = (function () {
    function SessionState() {
    }
    SessionState.new = "new";
    SessionState.negotiating = "negotiating";
    SessionState.authenticating = "authenticating";
    SessionState.established = "established";
    SessionState.finishing = "finishing";
    SessionState.finished = "finished";
    SessionState.failed = "failed";
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
    }
    WebSocketTransport.prototype.send = function (envelope) {
        this.ensureSocketOpen();
        this.webSocket.send(JSON.stringify(envelope));
    };
    WebSocketTransport.prototype.onEnvelope = function (envelope) { };
    WebSocketTransport.prototype.setStateListener = function (listener) {
        this.transportStateListener = listener;
    };
    WebSocketTransport.prototype.open = function (uri) {
        var _this = this;
        this.webSocket = new WebSocket(uri, "lime");
        this.webSocket.onmessage = function (e) {
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
            _this.onEnvelope(envelope);
        };
        this.webSocket.onopen = function (e) {
            if (_this.transportStateListener != null) {
                _this.transportStateListener.onOpen();
            }
        };
        this.webSocket.onclose = function (e) {
            if (_this.transportStateListener != null) {
                _this.transportStateListener.onClosed();
            }
            _this.webSocket = null;
        };
        this.webSocket.onerror = function (e) {
            if (_this.transportStateListener != null) {
                _this.transportStateListener.onError();
            }
            _this.webSocket = null;
            console.log(e);
        };
    };
    WebSocketTransport.prototype.close = function () {
        this.ensureSocketOpen();
        this.webSocket.close();
    };
    WebSocketTransport.prototype.ensureSocketOpen = function () {
        if (this.webSocket == null ||
            this.webSocket.readyState !== WebSocket.OPEN) {
            throw "The connection is not open";
        }
    };
    return WebSocketTransport;
})();
var Channel = (function () {
    function Channel(transport) {
        var _this = this;
        this.transport = transport;
        this.transport.onEnvelope = function (e) {
            if (e.hasOwnProperty("event")) {
                _this.onNotification(e);
            }
            else if (e.hasOwnProperty("content")) {
                _this.onMessage(e);
            }
            else if (e.hasOwnProperty("method")) {
                _this.onCommand(e);
            }
            else if (e.hasOwnProperty("state")) {
                _this.onSession(e);
            }
        };
        this.state = SessionState.new;
    }
    Channel.prototype.sendMessage = function (message) {
        if (this.state !== SessionState.established) {
            throw "Cannot send in the '" + this.state + "' state";
        }
        this.send(message);
    };
    Channel.prototype.onMessage = function (message) { };
    Channel.prototype.sendCommand = function (command) {
        if (this.state !== SessionState.established) {
            throw "Cannot send in the '" + this.state + "' state";
        }
        this.send(command);
    };
    Channel.prototype.onCommand = function (command) { };
    Channel.prototype.sendNotification = function (notification) {
        if (this.state !== SessionState.established) {
            throw "Cannot send in the '" + this.state + "' state";
        }
        this.send(notification);
    };
    Channel.prototype.onNotification = function (notification) { };
    Channel.prototype.sendSession = function (session) {
        if (this.state === SessionState.finished ||
            this.state === SessionState.failed) {
            throw "Cannot send in the '" + this.state + "' state";
        }
        this.send(session);
    };
    Channel.prototype.onSession = function (session) { };
    Channel.prototype.send = function (envelope) {
        this.transport.send(envelope);
    };
    return Channel;
})();
var ClientChannel = (function (_super) {
    __extends(ClientChannel, _super);
    function ClientChannel(transport) {
        var _this = this;
        _super.call(this, transport);
        _super.prototype.onSession = function (s) {
            _this.sessionId = s.id;
            _this.state = s.state;
            if (s.state === SessionState.established) {
                _this.localNode = s.to;
                _this.remoteNode = s.from;
            }
            else if (s.state === SessionState.finished || s.state === SessionState.failed) {
                try {
                    _this.transport.close();
                }
                catch (e) {
                    console.error(e);
                }
            }
        };
    }
    ClientChannel.prototype.startNewSession = function (sessionListener) {
    };
    return ClientChannel;
})(Channel);
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

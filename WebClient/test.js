function Sock(url) {
    this.url = url;
    this.sock = null;
    this.connected = false;
    this.timeout = null;
    this.receive = null;
    
    this.reset();
    this.open();
}

Sock.prototype = {
    open: function () {
        var me = this;

        if (me.sock) {
            me.sock.onclose = null;
            me.sock.close();
        }

        var sock = me.sock = new WebSocket(me.url);

        sock.onopen = function (e) {
            me.reset();
            me.connected = true;
            me.onopen && me.onopen(e);
        };

        sock.onerror = function (e) {
            sock.close();
        }

        sock.onclose = function (e) {
            me.connected = false;
            me.retry && me.retry();
            me.onclose && me.onclose(e);
        }

        sock.onmessage = function (evt) {
            me.onmessage && me.onmessage(JSON.parse(evt.data));
        }
    },

    send: function (obj) {
        this.sock.send(JSON.stringify(obj));
    },

    close: function () {
        this.retry = null;
        this.sock && this.sock.close();
    },

    reset: function () {
        this.retry = this._retryLoop;
        this.retryMs = 500;
    },

    _retryLoop: function () {
        var me = this;
        window.clearTimeout(me.timeout);
        me.timeout = window.setTimeout(function () {
            me.retryMs = Math.min(15000, me.retryMs += 500);
            me.open();
        }, me.retryMs);
    }
};


var ControllerManager = {
    _registry: {},

    register: function (name, fns) {
        name = name.toLowerCase() + '.';
        for (var fn in fns) {
            var prop = fns[fn];
            if (typeof (prop) === 'function' && fn[0] != '_') {
                this._registry[name + fn.toLowerCase()] = this._call(fns, fn);
            }
        }
    },

    invoke: function (fnName, data, context) {
        var me = this,
            fn = me._registry[fnName.toLowerCase()];

        if (fn)
            fn(data, context);
        else if (fn = me._registry['error.unknownroute'])
            fn({ fn: fnName, data: data }, context);
    },

    _call: function (obj, name) {
        return function (a, b) {
            return obj[name](a, b);
        }
    }
};

function SockApp(sock, controllers) {
    var me = this;

    me.sock = sock;
    me.controllers = controllers;

    sock.onmessage = function (msg) {
        me.controllers.invoke(msg.fn, msg.data, me);
    };
}

SockApp.prototype = {
    send: function (fn, data) {
        this.sock.send({ fn: fn, data: data });
    }
}

$(function () {

    function status(msg) {
        var d = $('<div />');
        d.text(msg);
        $('#status').append(d);
    }


    ControllerManager.register('auth', {
        authorized: function (data, context) {
            status('Logged in: ' + data.username);
        }
    });

    ControllerManager.register('notification', {
        handle: function (data, context) {
            status(data);
        }
    });

    ControllerManager.register('error', {
        handle: function (data, context) {
            status('ERROR: ' + data.message);
        },

        unknownRoute: function (data, context) {
            status('BADROUTE: ' + data.fn);
        }
    });

    var app = new SockApp(new Sock('ws://localhost:8181'), ControllerManager);
    
    app.sock.onopen = function () {
        $('#status').html(' ');
        status('Ready');
    };

    app.sock.onclose = function () {
        status('Closed');
    };
     
    $('#login-form').submit(function () {
        var username = $('#username').val();

        app.send('auth.login', { username: username });

        return false;
    });

    $('#message-form').submit(function () {
        var message = $('#message').val();

        app.send('notification.broadcast', {
            toUserIds: $('#toUserIds').val().split(','), 
            title: $('#title').val(),
            subTitle: $('#subTitle').val()
        });

        return false;
    })
});
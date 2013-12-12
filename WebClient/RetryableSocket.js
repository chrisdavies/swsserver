function RetryableSocket(url) {
    this.url = url;
    this.sock = null;
    this.connected = false;
    this.timeout = null;
    this.receive = null;

    this.reset();
    this.open();
}

RetryableSocket.prototype = {
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
            if (me.connected) {
                me.connected = false;
                me.onclose && me.onclose(e);
            }

            me.retry && me.retry();
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
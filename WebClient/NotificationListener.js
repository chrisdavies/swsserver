function NotificationListener(options) {
    var me = this;
    var maxLength = options.maxLength || 10;
    var router = new WebSocketRouter();
    var notifications = [];

    router.addRoute('error.handle', function (err) {
        alert(JSON.stringify(err));
    });

    router.addRoute('auth.authorized', function (msg, context) {
        context.send('notification.getLastN', 10);
    });

    router.addRoute('notification.handle', function (notification) {
        if (notification.length) {
            notifications = notification;
        } else {
            notifications.unshift(notification);
            if (notifications.length > maxLength) notifications.pop();
        }

        options.process && options.process(notifications);
    });
  
    var sock = this.sock = new RetryableSocket(options.url);

    router.attachTo(sock);

    me.sock.onopen = function () {
        sock.send({ fn: 'auth.login', data: { username: options.securityToken } });
        options.connected && options.connected();
    };
    me.sock.onclose = function () { options.disconnected && options.disconnected(); };
}

NotificationListener.prototype = {
    dispose: function () {
        this.sock.close();
    }
}
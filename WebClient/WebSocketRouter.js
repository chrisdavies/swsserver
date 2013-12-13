function WebSocketRouter() {
    this.routes = { };
}

WebSocketRouter.prototype = {
    addRoute: function(name, fn) {
        var me = this;

        function addNamedFn(name, fn) {
            me.routes[name.toLowerCase()] = fn;
        }

        function addNamedObj(name, obj) {
            name = name + '.';
            for (var fnName in obj) {
                var fn = obj[fnName];
                if (typeof (fn) === 'function' && fnName[0] != '_') {
                    me.routes[name + fnName.toLowerCase()] = me.invoker(obj, fnName);
                }
            }
        }

        function addRouteGroup(obj) {
            for (var name in obj) {
                var router = obj[name];
                if (typeof(router) === 'object') {
                    addNamedObj(name, router);
                }
            }
        }

        if (fn === undefined) addRouteGroup(name);
        else if (typeof(fn) === 'function') addNamedFn(name, fn);
        else addNamedObj(name, fn);
    },

    attachTo: function (socket) {
        var me = this;

        socket.onmessage = function (msg) {
            me.invoke(msg.fn, msg.data, socket);
        }
    },

    invoke: function (fnName, data, context) {
        var me = this,
            fn = me.routes[fnName.toLowerCase()];

        if (fn)
            fn(data, context);
        else if (fn = me.routes['error.unknownroute'])
            fn({ fn: fnName, data: data }, context);
    },

    invoker: function (obj, name) {
        return function (a, b) {
            return obj[name](a, b);
        }
    }
};
$(function () {
    function processNotificationsFor(userId) {
        var notifications = new NotificationListener({
            url: 'ws://localhost:8181',
            securityToken: userId,
            process: function (notifications) {
                var arr = notifications.map(function (n) {
                    return n.title;
                });
                $('#notifications').html('<div>' + arr.join('</div><div>') + '</div>');
            },
            connected: function () {
                $('#notifications').text('Connected...');
            },
            disconnected: function () {
                $('#notifications').text('Disconnected...');
            }
        });
    }

    $('#login-form').submit(function () {
        var username = $('#username').val();

        processNotificationsFor(username);

        return false;
    });
});
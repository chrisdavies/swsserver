using AutoMapper;
using SuperWebServer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Threading;

namespace SuperWebSocket.Samples.BasicConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var processor = new SuperMessageProcessor();
            processor.AddControllersFromAssemblies(typeof(Program).Assembly);

            using (var server = new SuperServer<ExampleSession>(8181, processor))
            {
                Console.WriteLine("SuperWebSocket server running.");
                Console.WriteLine("Press 'q' to quit.");

                var command = Console.ReadLine();
                while (command != "q")
                {
                    command = Console.ReadLine();
                }
            }
        }
    }

    public class ExampleSession : SuperSession
    {
        public void Authorized(string userId)
        {
            this.UserId = userId;
            AuthenticatedSessions.Add(this);
        }

        public override void Dispose()
        {
            AuthenticatedSessions.Remove(this);
            base.Dispose();
        }
    }

    public class AuthenticatedAttribute : SuperWebServer.BeforeExecuteAttribute
    {
        public override void BeforeExecute(object model, IBaseSession session)
        {
            if (!session.IsAuthenticated)
                SuperServerException.Throw(HttpStatusCode.Unauthorized, "Please login");
        }
    }

    public class SyscallAttribute : SuperWebServer.BeforeExecuteAttribute
    {
        public override void BeforeExecute(object model, IBaseSession session)
        {
            if (session.UserId != "sys")
                SuperServerException.Throw(HttpStatusCode.Forbidden, "Only the sys account can call this method.");
        }
    }

    public abstract class SuperController: ISuperController
    {
    }

    [Authenticated]
    public abstract class AuthenticatedSuperController : SuperController
    {
    }

    public class AuthController : SuperController
    {
        public string validUsernames = ";cdavies;jmoore;jgarwood;sys;";

        public void Login(LoginCredentials credentials, ExampleSession session)
        {
            if (session.IsAuthenticated)
                SuperServerException.Throw(HttpStatusCode.Forbidden, "You cannot reauthenticate on an authenticated connection");

            if (!validUsernames.Contains(";" + credentials.Username + ";"))
                SuperServerException.Throw(HttpStatusCode.Unauthorized, "Invalid username");

            session.Authorized(credentials.Username);

            session.Send(new WebSocketMessage("Auth.Authorized", credentials));
        }
    }

    public class LoginCredentials
    {
        public string Username { get; set; }

        public string Password { get; set; }
    }

    public class Notification
    {
        public string Type { get; set; }

        public string Title { get; set; }

        public string Subtitle { get; set; }
    }

    public class BroadcastNotification : Notification
    {
        public IEnumerable<string> ToUserIds { get; set; }
    }

    public class MessageHistory<T>
    {
        private HistoryRecord<T>[] records;
        private int startIndex;

        public MessageHistory(int maxSize)
        {
            this.startIndex = maxSize - 1;
            this.records = new HistoryRecord<T>[maxSize];
        }

        public void Add(IEnumerable<string> userIds, T message)
        {
            var record = new HistoryRecord<T> { UserIds = userIds, Record = message };

            lock (records) {
                startIndex = (startIndex + 1) % records.Length;
                records[startIndex] = record;
            }
        }

        public IEnumerable<T> GetLastN(int n, string userId)
        {
            return Records(userId).Take(n).Select(r => r.Record);
        }

        private IEnumerable<HistoryRecord<T>> Records(string userId)
        {
            var index = this.startIndex;
            for (var i = 0; i < records.Length; ++i)
            {
                var record = records[index];

                if (record == null) break;

                if (record.UserIds.Any(id => id == userId))
                    yield return record;

                index -= 1;
                if (index < 0) index = records.Length - 1;
            }
        }

        private class HistoryRecord<T>
        {
            public T Record { get; set; }

            public IEnumerable<string> UserIds { get; set; }
        }
    }

    public class NotificationController : AuthenticatedSuperController
    {
        private MessageHistory<Notification> history = new MessageHistory<Notification>(1000);

        [Syscall]
        public void Broadcast(BroadcastNotification broadcast, IBaseSession session)
        {
            var notification = broadcast.MapTo<Notification>();
            history.Add(broadcast.ToUserIds, notification);
            AuthenticatedSessions.Broadcast(new WebSocketMessage("Notification.Handle", notification, broadcast.ToUserIds));
        }

        public void GetLastN(int maxNotifications, IBaseSession session)
        {
            maxNotifications = Math.Min(100, Math.Max(0, maxNotifications));
            session.Send(new WebSocketMessage("Notification.Handle", history.GetLastN(maxNotifications, session.UserId)));
        }
    }

    public static class ObjectEx
    {
        public static T MapTo<T>(this object o) where T : new()
        {
            return Mapper.Map<T>(o);
        }
    }
}

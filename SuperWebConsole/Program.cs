using AutoMapper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Threading;
using WebSocketService.Server;
using WebSocketService.Sys;
using WebSocketService.JSON;

namespace SuperWebSocket.Samples.BasicConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var router = new MyRouter(new JSONSerializer());
            router.AddControllersFromAssemblies(typeof(Program).Assembly);

            using (var server = new WebSocketService<MySession>(8181, router, router))
            {
                Console.WriteLine("Running. Press 'q' to quit");
                while (Console.ReadLine() != "q") { }
            }
        }
    }

    public enum UserType
    {
        User,
        System
    }

    public class User
    {
        public UserType Type { get; set; }

        public string UserId { get; set; }
    }

    public class MySession : BasicSession
    {
        public UserType UserType { get; set; }

        public MySession(User user, IChannel channel, IBroadcaster broadcaster, ISerializer serializer)
            : base(user.UserId, channel, broadcaster, serializer)
        {
            this.UserType = user.Type;
        }
    }

    public class MyRouter : BasicRouter<MySession>
    {
        public MyRouter(ISerializer serializer)
            : base(serializer)
        {
        }

        public override MySession Create(IChannel channel)
        {
            var user = GetUser(channel);
            if (user == null) return null;

            var session = new MySession(user, channel, this.Sessions, this.Serializer);
            if (user.Type == UserType.System) return session;

            return this.Sessions.Add(session);
        }

        private User GetUser(IChannel channel)
        {
            var id = channel.Metadata["system"];
            if (id != null)
            {
                return new User { UserId = id, Type = UserType.System };
            }

            id = channel.Metadata["user"];
            if (id != null)
            {
                return new User { UserId = id, Type = UserType.User };
            }

            return null;
        }
    }
    
    public class SyscallAttribute : BeforeExecuteAttribute
    {
        public override void BeforeExecute(object model, ISession session)
        {
            var realSession = session as MySession;
            if (realSession == null || realSession.UserType != UserType.System)
                WebSocketException.ThrowForbidden("Only the sys account can call this method.");
        }
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

            lock (records)
            {
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

    public class NotificationController : IController
    {
        private MessageHistory<Notification> history = new MessageHistory<Notification>(1000);

        [Syscall]
        public void Broadcast(BroadcastNotification broadcast, ISession session)
        {
            var notification = broadcast.MapTo<Notification>();
            history.Add(broadcast.ToUserIds, notification);
            session.Broadcast(broadcast.ToUserIds, new OutgoingMessage("Notification.Handle", notification));
        }

        public void GetLastN(int maxNotifications, ISession session)
        {
            maxNotifications = Math.Min(100, Math.Max(0, maxNotifications));
            session.Write(new OutgoingMessage("Notification.Handle", history.GetLastN(maxNotifications, session.UserId)));
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

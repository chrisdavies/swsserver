using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.SocketBase;
using SuperWebServer;
using System.Reflection;
using System.Web;
using System.Net;
using System.Collections.Concurrent;
using AutoMapper;

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

    public class NotificationController : AuthenticatedSuperController
    {
        [Syscall]
        public void Broadcast(BroadcastNotification broadcast, IBaseSession session)
        {
            AuthenticatedSessions.Broadcast(new WebSocketMessage("Notification.Handle", broadcast.MapTo<Notification>(), broadcast.ToUserIds));
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

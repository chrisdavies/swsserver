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

namespace SuperWebSocket.Samples.BasicConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var processor = new SuperMessageProcessor();
            processor.Scan(typeof(Program).Assembly);

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

    public static class AuthenticatedSessions
    {
        private static ConcurrentDictionary<string, ConcurrentDictionary<EndPoint, IBaseSession>> authenticatedUsers =
               new ConcurrentDictionary<string, ConcurrentDictionary<EndPoint, IBaseSession>>(StringComparer.OrdinalIgnoreCase);

        public static void Add(IBaseSession session)
        {
            var userConnections = authenticatedUsers.GetOrAdd(session.UserId, s => new ConcurrentDictionary<EndPoint, IBaseSession>());
            userConnections.TryAdd(session.RemoteAddress, session);
        }

        public static void Remove(IBaseSession session)
        {
            if (session.IsAuthenticated)
            {
                var contexts = ContextsFor(session.UserId);
                if (contexts != null)
                {
                    contexts.TryRemove(session.RemoteAddress, out session);
                }
            }
        }

        private static ConcurrentDictionary<EndPoint, IBaseSession> ContextsFor(string userId)
        {
            ConcurrentDictionary<EndPoint, IBaseSession> contexts;
            authenticatedUsers.TryGetValue(userId, out contexts);
            return contexts;
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

    public abstract class SuperController: ISuperController
    {
        public virtual void BeforeExecute(object model, IBaseSession session)
        {
        }
    }

    public abstract class AuthorizedSuperController : SuperController
    {
        public override void BeforeExecute(object model, IBaseSession session)
        {
            if (!session.IsAuthenticated) 
                SuperServerException.Throw(HttpStatusCode.Unauthorized, "Please login");
        }
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

    public class TestController : AuthorizedSuperController
    {
        public void Say(string message, IBaseSession session)
        {
            session.Send(new WebSocketMessage("Say.Said", "Said " + message + " at  " + DateTime.Now, "cdavies", "jgarwood"));
        }
    }
}

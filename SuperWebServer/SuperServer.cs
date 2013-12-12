using SuperSocket.SocketBase.Config;
using SuperWebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SuperWebServer
{
    public class SuperServer<T> : IDisposable where T : ISuperSession, new()
    {
        private WebSocketServer server;

        public SuperServer(int port, IMessageProcessor processor)
        {
            server = new WebSocketServer();

            server.NewMessageReceived += (session, message) =>
            {
                processor.ProcessMessage(session.Items[0] as ISuperSession, message);
            };

            server.SessionClosed += (session, message) =>
            {
                var mySession = (ISuperSession)session.Items[0];
                processor.RemoveSession(mySession);
                mySession.Dispose();
            };

            server.NewSessionConnected += (session) =>
            {
                var newSession = new T();
                session.Items[0] = newSession;
                newSession.WebSocketSession = session;
                processor.AddSession(newSession);
            };

            if (!server.Setup(new ServerConfig { Port = port, MaxConnectionNumber = 100000 }) || !server.Start())
            {
                throw new SuperServerException("Server setup failed. Turn on SuperWebSockets logging for more details.");
            }
        }
        
        public void Dispose()
        {
            if (server != null)
            {
                server.Dispose();
                server = null;
            }
        }
    }

    public interface ISuperSession : IBaseSession
    {
        WebSocketSession WebSocketSession { get; set; }
    }

    public class SuperSession : BaseSession, ISuperSession
    {
        public WebSocketSession WebSocketSession { get; set; }

        public override EndPoint RemoteAddress { get { return WebSocketSession.RemoteEndPoint; } }

        public override void Send(string message)
        {
            WebSocketSession.Send(message);
        }

        public override void Close()
        {
            WebSocketSession.Close();
        }

        public override void Dispose() { }
    }
}

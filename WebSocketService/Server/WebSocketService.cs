using SuperSocket.SocketBase.Config;
using SuperWebSocket;
using System;
using System.Net;
using WebSocketService.Sys;

namespace WebSocketService.Server
{
    public class WebSocketService<T> : IDisposable where T : class, ISession
    {
        private const string SessionKey = "S";
        private WebSocketServer server;

        public WebSocketService(int port, IRouter<T> router, ISessionManager<T> sessions)
        {
            server = new WebSocketServer();

            server.NewMessageReceived += (sock, message) =>
            {
                T session = null;

                try
                {
                    session = Session(sock);
                    if (session != null)
                    {
                        router.Route(message, session);
                    }
                    else
                    {
                        sock.Close();
                    }
                }
                catch (Exception ex)
                {
                    if (session != null)
                    {
                        router.Error(ex, session);
                    }

                    sock.Close();
                }
            };

            server.SessionClosed += (sock, message) =>
            {
                T session = Session(sock);
                if (session != null)
                {
                    sessions.Remove(session);
                    session.Close();
                }
            };

            server.NewSessionConnected += (sock) =>
            {
                var session = sessions.Create(new WebSocketChannel(sock));
                if (session == null)
                {
                    sock.Close();
                }
                else
                {
                    sock.Items[SessionKey] = session;
                }
            };

            if (!server.Setup(new ServerConfig { Port = port, MaxConnectionNumber = 100000 }) || !server.Start())
            {
                WebSocketException.ThrowServerError("Server setup failed. Turn on SuperWebSockets logging for more details.");
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

        private T Session(WebSocketSession session)
        {
            object tSession;
            if (session.Items.TryGetValue(SessionKey, out tSession))
            {
                return (T)tSession;
            }

            return null;
        }
    }
}

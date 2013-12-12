using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SuperWebServer
{
    public static class AuthenticatedSessions
    {
        private static ConcurrentDictionary<string, ConcurrentDictionary<EndPoint, IBaseSession>> authenticatedUsers =
               new ConcurrentDictionary<string, ConcurrentDictionary<EndPoint, IBaseSession>>(StringComparer.OrdinalIgnoreCase);

        public static void Add(IBaseSession session)
        {
            var userConnections = authenticatedUsers.GetOrAdd(session.UserId, s => new ConcurrentDictionary<EndPoint, IBaseSession>());
            userConnections.TryAdd(session.RemoteAddress, session);
        }

        public static void Broadcast(WebSocketMessage message)
        {
            message.ToUserIds.ForEach(id =>
            {
                ConcurrentDictionary<EndPoint, IBaseSession> sessions;
                authenticatedUsers.TryGetValue(id, out sessions);
                if (sessions != null) sessions.Values.ForEach(session => session.Send(message));
            });
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
}

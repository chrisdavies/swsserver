using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace WebSocketService.Sys
{
    public class SessionManager<T> : IBroadcaster where T : ISession
    {
        private ConcurrentDictionary<string, ConcurrentDictionary<EndPoint, T>> activeSessions =
               new ConcurrentDictionary<string, ConcurrentDictionary<EndPoint, T>>(StringComparer.OrdinalIgnoreCase);

        public virtual T Add(T session)
        {
            var userConnections = activeSessions.GetOrAdd(session.UserId, s => new ConcurrentDictionary<EndPoint, T>());
            userConnections.TryAdd(session.Channel.RemoteEndPoint, session);
            return session;
        }

        public virtual void Broadcast(IEnumerable<string> toUserIds, string message)
        {
            toUserIds.ForEach(
                id => WithSessions(id, 
                    sessions => sessions.Values.ForEach(
                        session => session.Channel.Write(message))));
        }

        public virtual void Remove(T session)
        {
            WithSessions(session.UserId, sessions => sessions.TryRemove(session.Channel.RemoteEndPoint, out session));
        }

        private void WithSessions(string userId, Action<ConcurrentDictionary<EndPoint, T>> fn)
        {
             ConcurrentDictionary<EndPoint, T> sessions;
             if (activeSessions.TryGetValue(userId, out sessions)) fn(sessions);
        }
    }
}

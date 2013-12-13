using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketService.Sys
{
    public interface ISessionManager<T> where T : ISession
    {
        /// <summary>
        /// Adds a session. If the router rejects the session,
        /// it can return null.
        /// </summary>
        /// <param name="channel">The channel for which the session will be added.</param>
        /// <returns>The added session.</returns>
        T Create(IChannel channel);

        /// <summary>
        /// Removes a session.
        /// </summary>
        /// <param name="session">The session to be removed.</param>
        void Remove(T session);
    }
}

using System.Collections.Generic;

namespace WebSocketService.Sys
{
    public interface IBroadcaster
    {
        /// <summary>
        /// Broadcasts the specified message to sessions with the specified
        /// user ids.
        /// </summary>
        /// <param name="toUserIds">The ids of users whose sessions get the message.</param>
        /// <param name="message">The message to send.</param>
        void Broadcast(IEnumerable<string> toUserIds, string message);
    }
}

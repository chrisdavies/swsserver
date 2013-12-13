using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketService.Sys
{
    /// <summary>
    /// Represents an instance of a server session.
    /// </summary>
    public interface ISession 
    {
        /// <summary>
        /// Gets the user id. This does not have to be unique across the
        /// system, but cannont be null.
        /// </summary>
        string UserId { get; }

        /// <summary>
        /// Gets the channel object.
        /// </summary>
        IChannel Channel { get; }

        /// <summary>
        /// Writes an object out to the channel.
        /// </summary>
        /// <param name="obj">The object to be written.</param>
        void Write(object obj);

        /// <summary>
        /// Broadcasts the specified message to the specified user ids.
        /// </summary>
        /// <param name="toUserIds">The user ids to receive the broadcast.</param>
        /// <param name="message">The message to be broadcast.</param>
        void Broadcast(IEnumerable<string> toUserIds, object message);

        /// <summary>
        /// Closes this session.
        /// </summary>
        void Close();
    }
}

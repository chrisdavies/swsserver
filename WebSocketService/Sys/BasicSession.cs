using System.Collections.Generic;

namespace WebSocketService.Sys
{
    public class BasicSession : ISession
    {
        private IBroadcaster broadcaster;
        private ISerializer serializer;

        public string UserId { get; set; }

        public IChannel Channel { get; set; }

        public BasicSession(
            string userId, 
            IChannel channel, 
            IBroadcaster broadcaster, 
            ISerializer serializer)
        {
            this.UserId = userId;
            this.Channel = channel;
            this.broadcaster = broadcaster;
            this.serializer = serializer;
        }

        public virtual void Close()
        {
            this.Channel.Close();
        }

        public void Broadcast(IEnumerable<string> toUserIds, object message)
        {
            this.broadcaster.Broadcast(toUserIds, this.serializer.Serialize(message));
        }

        public void Write(object obj)
        {
            this.Channel.Write(this.serializer.Serialize(obj));
        }
    }
}

using SuperWebSocket;
using System.Collections.Specialized;
using System.Net;
using WebSocketService.Sys;

namespace WebSocketService.Server
{
    internal class WebSocketChannel : IChannel
    {
        private WebSocketSession sock;
        private IMetadataCollection metadata;

        public WebSocketChannel(WebSocketSession sock)
        {
            this.sock = sock;
            this.metadata = new SocketMetadata(sock.Cookies);
        }

        public IMetadataCollection Metadata
        {
            get { return this.metadata; }
        }

        public EndPoint RemoteEndPoint
        {
            get { return this.sock.RemoteEndPoint; }
        }

        public void Write(string message)
        {
            this.sock.Send(message);
        }

        public void Close()
        {
            this.sock.Close();
        }

        private class SocketMetadata : IMetadataCollection
        {
            private StringDictionary dict;

            public SocketMetadata(StringDictionary dict)
            {
                this.dict = dict;
            }

            public string this[string key]
            {
                get
                {
                    return dict[key];
                }
                set
                {
                    dict[key] = value;
                }
            }
        }
    }
}

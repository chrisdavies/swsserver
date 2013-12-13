using Newtonsoft.Json.Linq;

namespace WebSocketService.Sys
{
    public class IncomingMessage
    {
        public string Fn { get; set; }

        public JToken Data { get; set; }
    }
}

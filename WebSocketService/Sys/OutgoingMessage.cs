namespace WebSocketService.Sys
{
    public class OutgoingMessage
    {
        public string Fn { get; set; }

        public object Data { get; set; }

        public OutgoingMessage()
        {
        }

        public OutgoingMessage(string fn, object data)
        {
            this.Fn = fn;
            this.Data = data;
        }
    }
}

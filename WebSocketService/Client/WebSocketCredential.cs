namespace WebSocketService.Client
{
    public abstract class WebSocketCredential
    {
        public string Type { get; set; }
    }

    public class SystemCredential : WebSocketCredential
    {
        private string token;

        public SystemCredential(string token)
        {
            this.Type = "system";
            this.token = token;
        }

        public override string ToString()
        {
            return this.token;
        }
    }
}

using System.Net;

namespace WebSocketService.Sys
{
    public class ErrorMessage : OutgoingMessage
    {
        public ErrorMessage(string message)
            : this(HttpStatusCode.InternalServerError, message)
        {
        }

        public ErrorMessage(int code, string message)
            : this((HttpStatusCode)code, message)
        {
        }

        public ErrorMessage(HttpStatusCode code, string message)
            : base("error.handle", new ErrorData { Code = code, Message = message })
        {
        }

        public class ErrorData
        {
            public HttpStatusCode Code { get; set; }

            public string Message { get; set; }
        }
    }
}

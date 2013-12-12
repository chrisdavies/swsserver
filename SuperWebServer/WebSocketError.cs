using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;

namespace SuperWebServer
{
    public class WebSocketMessage
    {
        [JsonIgnore]
        public IEnumerable<string> ToUserIds { get; set; }

        public string Fn { get; set; }

        public object Data { get; set; }

        public WebSocketMessage(string method, object data, IEnumerable<string> toUserIds)
        {
            this.Fn = method;
            this.ToUserIds = toUserIds;
            this.Data = data;
        }

        public WebSocketMessage(string method, object data, params string[] toUserIds)
            : this(method, data, (IEnumerable<string>)toUserIds)
        {
        }
    }

    public class WebSocketError : WebSocketMessage
    {
        private const string Method = "error.handle";

        public WebSocketError(HttpStatusCode code, string message)
            : base(Method, new ExceptionMessage(code, message))
        {
        }

        public WebSocketError(Exception ex)
            : base(Method, new ExceptionMessage(ex))
        {
        }

        public WebSocketError(HttpException ex)
            : base(Method, new ExceptionMessage(ex))
        {
        }
    }

    public class ExceptionMessage
    {
        public int Code { get; set; }

        public string Message { get; set; }

        public IDictionary Data { get; set; }

        public ExceptionMessage(HttpStatusCode code, string message)
        {
            this.Code = (int)code;
            this.Message = message;
        }

        public ExceptionMessage(Exception ex)
            : this(HttpStatusCode.InternalServerError, ex.Message)
        {
        }

        public ExceptionMessage(HttpException ex)
        {
            this.Code = ex.GetHttpCode();
            this.Message = ex.Message;
            this.Data = ex.Data;
        }
    }
}

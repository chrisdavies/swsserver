using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SuperWebServer
{
    public interface IBaseSession : IDisposable
    {
        bool IsAuthenticated { get; }

        string UserId { get; }

        EndPoint RemoteAddress { get; }

        void Send(string message);

        void Send(object message);

        T Read<T>(string message);

        void Close();
    }

    public abstract class BaseSession : IBaseSession
    {
        private static JsonSerializer jserializer;

        static BaseSession()
        {
            jserializer = new JsonSerializer();
            jserializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
            jserializer.Converters.Add(new IsoDateTimeConverter());
        }

        public T Read<T>(string message)
        {
            using (var str = new StringReader(message))
            using (var jr = new JsonTextReader(str))
                return jserializer.Deserialize<T>(jr);
        }

        public void Send(object message)
        {
            using (var stw = new StringWriter())
            using (var jw = new JsonTextWriter(stw))
            {
                jserializer.Serialize(jw, message);
                jw.Close();
                this.Send(stw.ToString());
            }
        }

        public virtual string UserId { get; set; }

        public virtual bool IsAuthenticated { get { return this.UserId != null; } }

        public abstract EndPoint RemoteAddress { get; }

        public abstract void Send(string message);

        public abstract void Close();

        public abstract void Dispose();
    }
}

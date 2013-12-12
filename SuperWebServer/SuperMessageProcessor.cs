using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Web;

namespace SuperWebServer
{
    public class SuperMessageProcessor : IMessageProcessor
    {
        private ConcurrentDictionary<string, ControllerMethod> methods = new ConcurrentDictionary<string, ControllerMethod>(StringComparer.OrdinalIgnoreCase);
        
        public void AddControllersFromAssemblies(params Assembly[] assemblies)
        {
            assemblies
                .Select(a => a.GetTypesDerivedFrom<ISuperController>(true))
                .ForEach(types => types.ForEach(AddController));
        }

        public void AddSession(IBaseSession session) { }

        public void RemoveSession(IBaseSession session) { }

        public void ProcessMessage(IBaseSession session, string message)
        {
            try
            {
                var parsedMessage = session.Read<IncomingMessage>(message);
                ControllerMethod method;
                if (!methods.TryGetValue(parsedMessage.Fn, out method))
                {
                    session.Send(new WebSocketError(HttpStatusCode.NotFound, "Could not find a handler for '" + parsedMessage.Fn + "'."));
                }
                else
                {
                    method.Invoke(parsedMessage.Data, session);
                }
            }
            catch (TargetInvocationException ex)
            {
                var inner = ex.InnerException as HttpException;
                session.Send(inner != null ? new WebSocketError(inner) : new WebSocketError(ex.InnerException));
            }
            catch (HttpException ex)
            {
                session.Send(new WebSocketError(ex));
            }
            catch (Exception ex)
            {
                session.Send(new WebSocketError(ex));
            }
        }

        private void AddController(Type t)
        {
            var inst = (ISuperController)t.CreateInstance();
            var preFilters = inst.GetType().GetCustomAttributes<BeforeExecuteAttribute>(true);
            var postFilters = inst.GetType().GetCustomAttributes<AfterExecuteAttribute>(true);

            foreach (var method in t.GetMethods().Where(m =>
            {
                var parameters = m.GetParameters();
                return parameters.Length == 2 && typeof(IBaseSession).IsAssignableFrom(parameters[1].ParameterType);
            }))
            {
                methods.TryAdd(t.Name.Replace("Controller", string.Empty) + "." + method.Name, new ControllerMethod(inst, method, preFilters, postFilters));
            }
        }

        private class ControllerMethod
        {
            private MethodInfo method;
            private Type dataType;
            private ISuperController instance;
            private IEnumerable<BeforeExecuteAttribute> preFilters;
            private IEnumerable<AfterExecuteAttribute> postFilters;

            public ControllerMethod(ISuperController instance, MethodInfo method, IEnumerable<BeforeExecuteAttribute> preFilters, IEnumerable<AfterExecuteAttribute> postFilters)
            {
                this.instance = instance;
                this.method = method;
                this.dataType = method.GetParameters()[0].ParameterType;

                this.preFilters = method.GetCustomAttributes<BeforeExecuteAttribute>(true).Union(preFilters);
                this.postFilters = method.GetCustomAttributes<AfterExecuteAttribute>(true).Union(postFilters);
            }

            public void Invoke(JToken data, IBaseSession session)
            {
                var model = data.ToObject(dataType);

                if (preFilters !=  null) PreFilter(model, session);
                method.Invoke(instance, new object[] { model, session });
                if (postFilters != null) PostFilter(model, session);
            }

            private void PostFilter(object model, IBaseSession session)
            {
                postFilters.ForEach(filter => filter.AfterExecute(model, session));
            }

            private void PreFilter(object model, IBaseSession session)
            {
                preFilters.ForEach(filter => filter.BeforeExecute(model, session));
            }
        }

        private class IncomingMessage
        {
            public string Fn { get; set; }

            public JToken Data { get; set; }
        }
    }
}

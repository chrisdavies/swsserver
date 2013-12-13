using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Web;

namespace WebSocketService.Sys
{
    public abstract class BasicRouter<T> : ISessionManager<T>, IRouter<T> where T : ISession
    {
        private ConcurrentDictionary<string, ControllerMethod> methods = new ConcurrentDictionary<string, ControllerMethod>(StringComparer.OrdinalIgnoreCase);

        public ISerializer Serializer { get; set; }

        public SessionManager<T> Sessions { get; set; }

        public BasicRouter(ISerializer serializer)
        {
            this.Sessions = new SessionManager<T>();
            this.Serializer = serializer;
        }

        public abstract T Create(IChannel channel);

        public void AddControllersFromAssemblies(params Assembly[] assemblies)
        {
            assemblies
                .Select(a => a.GetTypesDerivedFrom<IController>(true))
                .ForEach(types => types.ForEach(AddController));
        }

        public virtual void Route(string message, T session)
        {
            try
            {
                var parsedMessage = Serializer.Deserialize<IncomingMessage>(message);
                ControllerMethod method;
                if (!methods.TryGetValue(parsedMessage.Fn, out method))
                {
                    session.Write(new ErrorMessage(HttpStatusCode.NotFound, "Could not find a handler for message '" + parsedMessage.Fn + "'."));
                }
                else
                {
                    method.Invoke(parsedMessage.Data, session);
                }
            }
            catch (TargetInvocationException ex)
            {
                var inner = ex.InnerException as HttpException;
                session.Write(inner != null ? new ErrorMessage(inner.GetHttpCode(), inner.Message) : new ErrorMessage((ex.InnerException ?? ex).Message));
            }
            catch (HttpException ex)
            {
                session.Write(new ErrorMessage(ex.Message));
            }
            catch (Exception ex)
            {
                session.Write(new ErrorMessage(ex.Message));
            }
        }

        public virtual void Error(Exception ex, T session)
        {
            Console.WriteLine(ex.ToString());
        }

        public virtual void Remove(T session)
        {
            this.Sessions.Remove(session);
        }

        private void AddController(Type t)
        {
            var inst = (IController)t.CreateInstance();
            var preFilters = inst.GetType().GetCustomAttributes<BeforeExecuteAttribute>(true);
            var postFilters = inst.GetType().GetCustomAttributes<AfterExecuteAttribute>(true);

            foreach (var method in t.GetMethods().Where(m =>
            {
                var parameters = m.GetParameters();
                return parameters.Length == 2 && typeof(ISession).IsAssignableFrom(parameters[1].ParameterType);
            }))
            {
                methods.TryAdd(t.Name.Replace("Controller", string.Empty) + "." + method.Name, new ControllerMethod(inst, method, preFilters, postFilters));
            }
        }

        private class ControllerMethod
        {
            private MethodInfo method;
            private Type dataType;
            private IController instance;
            private IEnumerable<BeforeExecuteAttribute> preFilters;
            private IEnumerable<AfterExecuteAttribute> postFilters;

            public ControllerMethod(IController instance, MethodInfo method, IEnumerable<BeforeExecuteAttribute> preFilters, IEnumerable<AfterExecuteAttribute> postFilters)
            {
                this.instance = instance;
                this.method = method;
                this.dataType = method.GetParameters()[0].ParameterType;

                this.preFilters = method.GetCustomAttributes<BeforeExecuteAttribute>(true).Union(preFilters);
                this.postFilters = method.GetCustomAttributes<AfterExecuteAttribute>(true).Union(postFilters);
            }

            public void Invoke(JToken data, ISession session)
            {
                var model = data.ToObject(dataType);

                if (preFilters != null) PreFilter(model, session);
                method.Invoke(instance, new object[] { model, session });
                if (postFilters != null) PostFilter(model, session);
            }

            private void PostFilter(object model, ISession session)
            {
                postFilters.ForEach(filter => filter.AfterExecute(model, session));
            }

            private void PreFilter(object model, ISession session)
            {
                preFilters.ForEach(filter => filter.BeforeExecute(model, session));
            }
        }
    }
}

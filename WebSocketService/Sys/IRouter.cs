using System;

namespace WebSocketService.Sys
{
    public interface IRouter<T> where T : ISession
    {
        void Route(string message, T session);

        void Error(Exception ex, T session);
    }
}

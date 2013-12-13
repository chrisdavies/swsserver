using System;

namespace WebSocketService.Sys
{
    public abstract class AfterExecuteAttribute : Attribute
    {
        public abstract void AfterExecute(object model, ISession session);
    }
}

using System;

namespace WebSocketService.Sys
{
    public abstract class BeforeExecuteAttribute : Attribute
    {
        public abstract void BeforeExecute(object model, ISession session);
    }
}

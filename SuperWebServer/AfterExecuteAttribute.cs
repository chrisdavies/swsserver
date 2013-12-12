using System;

namespace SuperWebServer
{
    public abstract class AfterExecuteAttribute : Attribute
    {
        public abstract void AfterExecute(object model, IBaseSession session);
    }
}

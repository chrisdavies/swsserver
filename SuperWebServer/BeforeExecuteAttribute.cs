using System;

namespace SuperWebServer
{
    public abstract class BeforeExecuteAttribute : Attribute
    {
        public abstract void BeforeExecute(object model, IBaseSession session);
    }
}

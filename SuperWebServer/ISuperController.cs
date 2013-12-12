namespace SuperWebServer
{
    public interface ISuperController
    {
        void BeforeExecute(object model, IBaseSession session);
    }
}

namespace SuperWebServer
{
    public interface IMessageProcessor
    {
        void AddSession(IBaseSession session);
        void RemoveSession(IBaseSession session);
        void ProcessMessage(IBaseSession session, string message);
    }
}

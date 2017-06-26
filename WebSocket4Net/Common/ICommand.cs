namespace WebSocket4Net.Common
{
    public interface ICommand
    {
        string Name { get; }
    }

    public interface ICommand<in TSession, in TFrame> : ICommand where TFrame : IWebSocketFrame
    {
        void ExecuteCommand(TSession session, TFrame commandInfo);
    }
}

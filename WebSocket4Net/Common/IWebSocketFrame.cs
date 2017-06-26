namespace WebSocket4Net.Common
{
    public interface IWebSocketFrame
    {
        string Key { get; }
    }

    public interface IWebSocketFrame<out TCommandData> : IWebSocketFrame
    {
        TCommandData Data { get; }
    }
}

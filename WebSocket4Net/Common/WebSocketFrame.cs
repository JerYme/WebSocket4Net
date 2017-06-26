namespace WebSocket4Net.Common
{
    public abstract class WebSocketFrame<TCommandData> : IWebSocketFrame<TCommandData>
    {
        protected WebSocketFrame(string key, TCommandData data)
        {
            Key = key;
            Data = data;
        }

        public TCommandData Data { get; }
        public string Key { get; }

    }
}

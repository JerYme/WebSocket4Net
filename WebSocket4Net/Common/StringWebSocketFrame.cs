namespace WebSocket4Net.Common
{
    public class StringWebSocketFrame : WebSocketFrame<string>
    {
        public StringWebSocketFrame(string key, string data, string[] parameters)
            : base(key, data)
        {
            Parameters = parameters;
        }

        public string[] Parameters { get; }

        public string GetFirstParam() => Parameters.Length > 0 ? Parameters[0] : string.Empty;

        public string this[int index] => Parameters[index];
    }
}

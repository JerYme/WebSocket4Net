namespace WebSocket4Net.Common
{
    public class BinaryWebSocketFrame : WebSocketFrame<byte[]>
    {
        public BinaryWebSocketFrame(string key, byte[] data) : base(key, data)
        {
        }
    }
}

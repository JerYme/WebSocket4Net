using WebSocket4Net.Common;

namespace WebSocket4Net.Protocol
{
    class DraftHybi10HandshakeReader : HandshakeReader
    {
        public DraftHybi10HandshakeReader(WebSocket websocket)
            : base(websocket)
        {

        }

        public override WebSocketFrameProcessed ProcessWebSocketFrame(ArrayHolder<byte> ah, int offset, int length)
        {
            var frameProcessed = base.ProcessWebSocketFrame(ah, offset, length);
            if (!frameProcessed) return frameProcessed;

            //If bad request, NextCommandReader will still be this HandshakeReader
            return !BadRequestCode.Equals(frameProcessed.Frame?.Key) ? frameProcessed : WebSocketFrameProcessed.Fail(frameProcessed.LengthToProcess);
        }
    }
}

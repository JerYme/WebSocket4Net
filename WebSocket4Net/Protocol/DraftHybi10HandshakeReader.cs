namespace WebSocket4Net.Protocol
{
    class DraftHybi10HandshakeReader : HandshakeReader
    {
        public DraftHybi10HandshakeReader(WebSocket websocket)
            : base(websocket)
        {

        }

        public override WebSocketFrame BuildHandShakeFrame(byte[] readBuffer, int offset, int length, out int lengthToProcess, out bool success)
        {
            var cmdInfo = base.BuildHandShakeFrame(readBuffer, offset, length, out lengthToProcess, out success);
            if (!success) return null;

            //If bad request, NextCommandReader will still be this HandshakeReader
            success = !BadRequestCode.Equals(cmdInfo.Key);
            return cmdInfo;
        }
    }
}

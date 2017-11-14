namespace WebSocket4Net.Protocol.FrameReader
{
    class PayloadLengthFrameReader : FrameReader
    {
        public override ProcessFrame Process(int index, WebSocketDataFrame frame)
        {
            if (frame.ArrayLength < 2)
            {
                return ProcessFrame.Fail(index, this, frame.ArrayLength - index);
            }

            var nextFrameReader = frame.PayloadLength < 126 ? (frame.HasMask ? MaskKey : PayloadData) : ExtendedPayloadLength;
            return nextFrameReader.Process(2, frame);
        }
    }
}

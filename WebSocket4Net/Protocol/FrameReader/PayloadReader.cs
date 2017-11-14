namespace WebSocket4Net.Protocol.FrameReader
{
    class PayloadFrameReader : FrameReader
    {
        public override ProcessFrame Process(int index, WebSocketDataFrame frame)
        {
            frame.PayloadIndex = index;
            var targetSize = index + frame.ActualPayloadLength;

            if (frame.ArrayLength < targetSize)
            {
                return ProcessFrame.Fail(index, this);
            }

            return ProcessFrame.Pass(index,null, frame.ArrayLength - targetSize);
        }
    }
}

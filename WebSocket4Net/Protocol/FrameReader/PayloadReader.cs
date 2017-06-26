namespace WebSocket4Net.Protocol.FrameReader
{
    class PayloadFrameReader : FrameReader
    {
        public override ProcessFrame Process(int frameIndex, WebSocketDataFrame frame)
        {
            var targetSize = frameIndex + frame.ActualPayloadLength;

            if (frame.ArrayLength < targetSize)
            {
                return ProcessFrame.Fail(frameIndex, this);
            }

            return ProcessFrame.Pass(frameIndex,null, frame.ArrayLength - targetSize);
        }
    }
}

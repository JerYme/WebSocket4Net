namespace WebSocket4Net.Protocol.FrameReader
{
    class MaskKeyFrameReader : FrameReader
    {
        public override ProcessFrame Process(int frameIndex, WebSocketDataFrame frame)
        {
            int endMask = frameIndex + 4;

            if (frame.ArrayLength < endMask)
            {
                return ProcessFrame.Fail(frameIndex, this, frame.ArrayLength - frameIndex);
            }

            frame.MaskKey = frame.ArrayView.ToArrayData(frameIndex, 4);

            if (frame.ActualPayloadLength == 0)
            {
                return ProcessFrame.Pass(frameIndex, null, frame.ArrayLength - endMask);
            }

            if (frame.ArrayLength > endMask)
                return new PayloadFrameReader().Process(endMask, frame);

            return ProcessFrame.Pass(frameIndex);
        }
    }
}

namespace WebSocket4Net.Protocol.FrameReader
{
    class MaskKeyFrameReader : FrameReader
    {
        public override ProcessFrame Process(int index, WebSocketDataFrame frame)
        {
            int endMask = index + 4;

            if (frame.ArrayLength < endMask)
            {
                return ProcessFrame.Fail(index, this, frame.ArrayLength - index);
            }

            frame.MaskKey = frame.ArrayView.ToArrayData(index, 4);

            if (frame.ActualPayloadLength == 0)
            {
                return ProcessFrame.Pass(index, null, frame.ArrayLength - endMask);
            }

            if (frame.ArrayLength > endMask)
                return Payload.Process(endMask, frame);

            return ProcessFrame.Pass(index);
        }
    }
}

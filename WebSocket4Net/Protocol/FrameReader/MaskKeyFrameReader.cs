namespace WebSocket4Net.Protocol.FrameReader
{
    class MaskKeyFrameReader : FrameReader
    {
        public override ProcessFrame Process(int index, WebSocketDataFrame frame)
        {
            const int maskLength = 4;
            if (frame.ArrayLength < index + maskLength)
                return ProcessFrame.Fail(index, this, frame.ArrayLength - index);

            frame.MaskKey = frame.ArrayView.ToArrayData(index, 4);
            return PayloadData.Process(index + maskLength, frame);
        }
    }
}

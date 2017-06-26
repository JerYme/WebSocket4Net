namespace WebSocket4Net.Protocol.FrameReader
{
    class HeaderFrameReader : FrameReader
    {
        public override ProcessFrame Process(int frameIndex, WebSocketDataFrame frame)
        {
            if (frame.ArrayLength < 2)
            {
                return ProcessFrame.Fail(frameIndex,this, frame.ArrayLength - frameIndex);
            }

                    IFrameReader  nextFrameReader;
            if (frame.PayloadLength < 126)
            {
                if (frame.HasMask)
                {
                    nextFrameReader = MaskKey;
                }
                else
                {
                    if (frame.ActualPayloadLength == 0)
                    {
                        return ProcessFrame.Pass(frameIndex,null, frame.ArrayLength - 2);
                    }

                    nextFrameReader = Payload;
                }
            }
            else
            {
                nextFrameReader = Extension;
            }

            if (frame.ArrayLength > 2)
                return nextFrameReader.Process(2, frame);

            return ProcessFrame.Pass(frameIndex);
        }
    }
}

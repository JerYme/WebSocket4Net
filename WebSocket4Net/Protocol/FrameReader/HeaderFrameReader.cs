namespace WebSocket4Net.Protocol.FrameReader
{
    class HeaderFrameReader : FrameReader
    {
        public override ProcessFrame Process(int index, WebSocketDataFrame frame)
        {
            if (frame.ArrayLength < 2)
            {
                return ProcessFrame.Fail(index, this, frame.ArrayLength - index);
            }

            IFrameReader nextFrameReader;
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
                        return ProcessFrame.Pass(index, null, frame.ArrayLength - 2);
                    }

                    nextFrameReader = Payload;
                }
            }
            else
            {
                nextFrameReader = ExtendedPayload;
            }

            if (frame.ArrayLength > 2)
                return nextFrameReader.Process(2, frame);

            return ProcessFrame.Pass(index);
        }
    }
}

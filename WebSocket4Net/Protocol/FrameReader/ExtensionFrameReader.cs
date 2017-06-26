
namespace WebSocket4Net.Protocol.FrameReader
{

    class ExtensionFrameReader : FrameReader
    {
        public override ProcessFrame Process(int frameIndex, WebSocketDataFrame frame)
        {
            int endExtension = 2;

            if (frame.PayloadLength == 126)
                endExtension += 2;
            else
                endExtension += 8;

            if (frame.ArrayLength < endExtension)
            {
                return ProcessFrame.Fail(frameIndex, this, frame.ArrayLength - frameIndex);
            }

            IFrameReader nextFrameReader;
            if (frame.HasMask)
            {
                nextFrameReader = MaskKey;
            }
            else
            {
                if (frame.ActualPayloadLength == 0)
                {
                    return ProcessFrame.Pass(frameIndex, null, frame.ArrayLength - endExtension);
                }

                nextFrameReader = Payload;
            }

            if (frame.ArrayLength > endExtension)
                return nextFrameReader.Process(endExtension, frame);

            return ProcessFrame.Pass(frameIndex);
        }
    }
}

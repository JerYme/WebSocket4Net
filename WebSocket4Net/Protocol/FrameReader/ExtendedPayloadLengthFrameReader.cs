
namespace WebSocket4Net.Protocol.FrameReader
{

    class ExtendedPayloadLengthFrameReader : FrameReader
    {
        public override ProcessFrame Process(int index, WebSocketDataFrame frame)
        {
            var lengthExtended = frame.PayloadLength == 126 ? 2 : 2 + 6;

            if (frame.ArrayLength < index + lengthExtended)
            {
                return ProcessFrame.Fail(index, this, frame.ArrayLength - index);
            }

            return (frame.HasMask ? MaskKey : PayloadData).Process(index + lengthExtended, frame);
        }
    }
}

using System;

namespace WebSocket4Net.Protocol.FrameReader
{
    class PayloadDataFrameReader : FrameReader
    {
        public override ProcessFrame Process(int index, WebSocketDataFrame frame)
        {
            frame.PayloadIndex = index;
            var totalLength = index + frame.ActualPayloadLength;

            return frame.ArrayLength < totalLength 
                ? ProcessFrame.Fail(index, this) 
                : ProcessFrame.Pass(index,null, frame.ArrayLength - totalLength);
        }
    }
}

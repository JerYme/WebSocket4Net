namespace WebSocket4Net.Protocol.FrameReader
{
    abstract class FrameReader : IFrameReader
    {
        static FrameReader()
        {
            PayloadLength = new PayloadLengthFrameReader();
            ExtendedPayloadLength = new ExtendedPayloadLengthFrameReader();
            MaskKey = new MaskKeyFrameReader();
            PayloadData = new PayloadDataFrameReader();
        }

        public abstract ProcessFrame Process(int index, WebSocketDataFrame frame);

        public static IFrameReader Root => PayloadLength;

        protected static IFrameReader PayloadLength { get; }

        protected static IFrameReader ExtendedPayloadLength { get; }

        protected static IFrameReader MaskKey { get; }

        protected static IFrameReader PayloadData { get; }
    }
}

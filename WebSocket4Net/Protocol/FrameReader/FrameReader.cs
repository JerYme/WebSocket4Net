namespace WebSocket4Net.Protocol.FrameReader
{
    abstract class FrameReader : IFrameReader
    {
        static FrameReader()
        {
            Header = new HeaderFrameReader();
            ExtendedPayload = new ExtendedPayloadFrameReader();
            MaskKey = new MaskKeyFrameReader();
            Payload = new PayloadFrameReader();
        }

        public abstract ProcessFrame Process(int index, WebSocketDataFrame frame);

        public static IFrameReader Root => Header;

        protected static IFrameReader Header { get; }

        protected static IFrameReader ExtendedPayload { get; }

        protected static IFrameReader MaskKey { get; }

        protected static IFrameReader Payload { get; }
    }
}

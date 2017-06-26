namespace WebSocket4Net.Protocol.FrameReader
{
    public interface IFrameReader
    {
        ProcessFrame Process(int frameIndex, WebSocketDataFrame frame);
    }

    public struct ProcessFrame
    {
        private readonly bool _success;
        public readonly int FrameIndex;
        public readonly int LengthToProcess;
        public readonly IFrameReader Reader;

        public static ProcessFrame Pass(int frameIndex, IFrameReader reader = null, int lengthToProcess = 0) => new ProcessFrame(true, frameIndex, reader, lengthToProcess);
        public static ProcessFrame Fail(int frameIndex, IFrameReader reader = null, int lengthToProcess = 0) => new ProcessFrame(false, frameIndex, reader, lengthToProcess);

        public ProcessFrame(bool success, int frameIndex, IFrameReader reader, int lengthToProcess)
        {
            _success = success;
            FrameIndex = frameIndex;
            Reader = reader;
            LengthToProcess = lengthToProcess;
        }

        public static implicit operator bool(ProcessFrame frame) => frame._success;
        public static implicit operator int(ProcessFrame frame) => frame.FrameIndex;
    }

}

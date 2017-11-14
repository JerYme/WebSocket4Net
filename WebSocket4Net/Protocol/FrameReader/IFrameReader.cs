namespace WebSocket4Net.Protocol.FrameReader
{
    public interface IFrameReader
    {
        ProcessFrame Process(int index, WebSocketDataFrame frame);
    }


    public struct ProcessFrame
    {
        private readonly bool _success;
        public readonly int Index;
        public readonly int LengthToProcess;
        public readonly IFrameReader Reader;

        public static ProcessFrame Pass(int index, IFrameReader reader = null, int lengthToProcess = 0) => new ProcessFrame(true, index, reader, lengthToProcess);
        public static ProcessFrame Fail(int index, IFrameReader reader = null, int lengthToProcess = 0) => new ProcessFrame(false, index, reader, lengthToProcess);

        public ProcessFrame(bool success, int index, IFrameReader reader, int lengthToProcess)
        {
            _success = success;
            Index = index;
            Reader = reader;
            LengthToProcess = lengthToProcess;
        }

        public static implicit operator bool(ProcessFrame frame) => frame._success;
        public static implicit operator int(ProcessFrame frame) => frame.Index;
    }

}

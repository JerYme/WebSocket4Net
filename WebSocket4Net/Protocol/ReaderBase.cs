using System;
using WebSocket4Net.Common;

namespace WebSocket4Net.Protocol
{
    public abstract class HandshakeReaderBase : ReaderBase
    {
        protected HandshakeReaderBase(WebSocket websocket) : base(websocket)
        {
        }

        protected HandshakeReaderBase(WebSocket websocket, ArrayView<byte> arrayView) : base(websocket, arrayView)
        {
        }


        public abstract WebSocketFrame BuildHandShakeFrame(byte[] readBuffer, int offset, int length, out int lengthToProcess, out bool success);

    }
    public abstract class DataReaderBase : ReaderBase
    {
        protected DataReaderBase(WebSocket websocket) : base(websocket)
        {
        }

        protected DataReaderBase(WebSocket websocket, ArrayView<byte> arrayView) : base(websocket, arrayView)
        {
        }

        public virtual new ArrayChunk<byte> AddChunk(byte[] readBuffer, int offset, int length, bool copy) => base.AddChunk(readBuffer, offset, length, true);
        public virtual new ArrayChunk<byte> AddChunk(ArrayChunk<byte> segment) => base.AddChunk(segment);

        public abstract bool Process(out int lengthToProcess);
        public abstract WebSocketFrame BuildWebSocketFrame();
        public abstract void ResetDataFrame();

        private bool? _skipData;

        public virtual ProcessWebSocketFrame TryBuildWebSocketFrame(ArrayHolder<byte> ah, int offset, int length)
        {
            var arrayChunk = AddChunk(ArrayChunk<byte>.New(_skipData == true ? null : ah, offset, length, ArrayView.Length));
            if (arrayChunk != null && _skipData == null && ArrayView.Length >= 2)
            {
                var actualPayloadLength = new WebSocketDataFrame(ArrayView).ActualPayloadLength;
                if (actualPayloadLength > 1024 * 1024 * 25)
                {
                    _skipData = true;
                }
            }

            int lengthToProcess;
            var processed = Process(out lengthToProcess);
            if (processed)
            {
                var webSocketFrame = BuildWebSocketFrame();
                if (_skipData != true) webSocketFrame?.Decode(lengthToProcess);
                ResetDataFrame();

                _skipData = null;
                return ProcessWebSocketFrame.Pass(webSocketFrame, lengthToProcess);
            }
            return ProcessWebSocketFrame.Fail(lengthToProcess);
        }

        public virtual void Clear()
        {

        }

        public struct ProcessWebSocketFrame
        {
            private readonly bool _success;
            public readonly int LengthToProcess;
            public readonly WebSocketFrame Frame;

            public static ProcessWebSocketFrame Pass(WebSocketFrame reader = null, int lengthToProcess = 0) => new ProcessWebSocketFrame(true, reader, lengthToProcess);
            public static ProcessWebSocketFrame Fail(int lengthToProcess = 0) => new ProcessWebSocketFrame(false, null, lengthToProcess);

            public ProcessWebSocketFrame(bool success, WebSocketFrame reader, int lengthToProcess)
            {
                _success = success;
                Frame = reader;
                LengthToProcess = lengthToProcess;
            }

            public static implicit operator bool(ProcessWebSocketFrame frame) => frame._success;
            public static implicit operator int(ProcessWebSocketFrame frame) => frame.LengthToProcess;
            public static implicit operator WebSocketFrame(ProcessWebSocketFrame frame) => frame.Frame;
        }

    }

    public abstract class ReaderBase : IClientCommandReader<WebSocketFrame>
    {
        protected WebSocket WebSocket { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReaderBase"/> class.
        /// </summary>
        /// <param name="websocket">The websocket.</param>
        protected ReaderBase(WebSocket websocket) : this(websocket, new ArrayView<byte>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReaderBase" /> class.
        /// </summary>
        /// <param name="websocket">The websocket.</param>
        /// <param name="arrayView">The segment list.</param>
        protected ReaderBase(WebSocket websocket, ArrayView<byte> arrayView)
        {
            WebSocket = websocket;
            ArrayView = arrayView ?? new ArrayView<byte>();
        }

        /// <summary>
        /// Gets the buffer segments which can help you parse your commands conviniently.
        /// </summary>
        public virtual ArrayView<byte> ArrayView { get; }

        /// <summary>
        /// Adds the segment.
        /// </summary>
        /// <param name="readBuffer">The read buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <param name="copy">if set to <c>true</c> [copy].</param>
        /// <returns></returns>
        protected ArrayChunk<byte> AddChunk(byte[] readBuffer, int offset, int length, bool copy) => ArrayView.AddChunk(readBuffer, offset, length, copy);
        /// <summary>
        /// Adds the segment.
        /// </summary>
        /// <param name="segment">The segment.</param>
        /// <returns></returns>
        protected ArrayChunk<byte> AddChunk(ArrayChunk<byte> segment) => ArrayView.AddChunk(segment);

        /// <summary>
        /// Clears the segments.
        /// </summary>
        protected void ClearSegments() => ArrayView.Clear();
    }
}

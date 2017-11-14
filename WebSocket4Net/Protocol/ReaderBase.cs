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


        protected bool? _skipData;

        public abstract WebSocketFrameProcessed TryBuildWebSocketFrame(ArrayHolder<byte> ah, int offset, int length);

        public virtual void Clear()
        {
        }

        public struct WebSocketFrameProcessed
        {
            private readonly bool _success;
            public readonly int LengthToProcess;
            public readonly WebSocketFrame Frame;

            public static WebSocketFrameProcessed Pass(WebSocketFrame reader = null, int lengthToProcess = 0) => new WebSocketFrameProcessed(true, reader, lengthToProcess);
            public static WebSocketFrameProcessed Fail(int lengthToProcess = 0) => new WebSocketFrameProcessed(false, null, lengthToProcess);

            public WebSocketFrameProcessed(bool success, WebSocketFrame reader, int lengthToProcess)
            {
                _success = success;
                Frame = reader;
                LengthToProcess = lengthToProcess;
            }

            public static implicit operator bool(WebSocketFrameProcessed frameProcessed) => frameProcessed._success;
            public static implicit operator int(WebSocketFrameProcessed frameProcessed) => frameProcessed.LengthToProcess;
            public static implicit operator WebSocketFrame(WebSocketFrameProcessed frameProcessed) => frameProcessed.Frame;
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
        /// Adds the chunk.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <param name="copy">if set to <c>true</c> [copy].</param>
        /// <returns></returns>
        protected ArrayChunk<byte> AddChunk(byte[] array, int offset, int length, bool copy) => ArrayView.AddChunk(array, offset, length, copy);

        /// <summary>
        /// Adds the segment.
        /// </summary>
        /// <param name="segment">The segment.</param>
        /// <returns></returns>
        protected ArrayChunk<byte> AddChunk(ArrayChunk<byte> segment) => ArrayView.AddChunk(segment);

        /// <summary>
        /// Clears the segments.
        /// </summary>
        protected void ClearBuffers() => ArrayView.Clear();
    }
}

using WebSocket4Net.Common;
using WebSocket4Net.Protocol.FrameReader;

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

        public virtual new ArrayChunk<byte> AddSegment(byte[] readBuffer, int offset, int length, bool copy) => base.AddSegment(readBuffer, offset, length, true);
        public virtual new ArrayChunk<byte> AddSegment(ArrayChunk<byte> segment) => base.AddSegment(segment);

        public abstract bool Process(out int lengthToProcess);
        public abstract WebSocketFrame BuildWebSocketFrame(int lengthToProcess);
        public abstract void ResetWebSocketFrame();
        public virtual void Clear()
        {
            
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
            ArrayView = arrayView;
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
        protected ArrayChunk<byte> AddSegment(byte[] readBuffer, int offset, int length, bool copy) => ArrayView.AddChunk(readBuffer, offset, length, copy);
        /// <summary>
        /// Adds the segment.
        /// </summary>
        /// <param name="segment">The segment.</param>
        /// <returns></returns>
        protected ArrayChunk<byte> AddSegment(ArrayChunk<byte> segment) => ArrayView.AddChunk(segment);

        /// <summary>
        /// Clears the segments.
        /// </summary>
        protected void ClearSegments() => ArrayView.Clear();
    }
}

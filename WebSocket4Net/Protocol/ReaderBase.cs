using System;
using WebSocket4Net.Common;

namespace WebSocket4Net.Protocol
{
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
        /// 
        /// </summary>
        /// <param name="ah"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public abstract WebSocketFrameProcessed ProcessWebSocketFrame(ArrayHolder<byte> ah, int offset, int length);
    }
}

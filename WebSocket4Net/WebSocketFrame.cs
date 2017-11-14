using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using WebSocket4Net.Common;
using WebSocket4Net.Protocol;

namespace WebSocket4Net
{
    public class WebSocketFrame : IWebSocketFrame
    {
        private readonly IList<WebSocketDataFrame> _dataFrames;

        public WebSocketFrame()
        {

        }

        public WebSocketFrame(string key)
        {
            Key = key;
        }

        public WebSocketFrame(string key, string text)
        {
            Key = key;
            Text = text;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketFrame" /> class.
        /// </summary>
        /// <param name="dataFrame">The frames.</param>
        public WebSocketFrame(params WebSocketDataFrame[] dataFrame)
           : this((IList<WebSocketDataFrame>)dataFrame)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketFrame" /> class.
        /// </summary>
        /// <param name="dataFrames">The frames.</param>
        public WebSocketFrame(IList<WebSocketDataFrame> dataFrames)
        {
            if (dataFrames == null) throw new ArgumentNullException(nameof(dataFrames));
            _dataFrames = dataFrames;
        }

        public void Decode()
        {
            if (_dataFrames == null) return;
            Decode(_dataFrames);
        }

        private void Decode(IList<WebSocketDataFrame> dataFrames)
        {
            var opCode = dataFrames[0].OpCode;
            Key = opCode.ToString();
            if (opCode == OpCode.Close)
            {
                var closeFrame = dataFrames[0];
                if (closeFrame.HasMask) closeFrame.DecodeMask();

                var length = closeFrame.ActualPayloadLength;
                var offset = closeFrame.PayloadIndex;

                using (var sb = StringBuilderShared.Acquire(closeFrame, 2))
                {
                    if (length >= 2)
                    {
                        var closeStatusCode = closeFrame.ArrayView.ToArrayData(closeFrame.PayloadIndex, 2);
                        CloseStatusCode = (short)(closeStatusCode[0] * 256 + closeStatusCode[1]);

                        if (length > 2)
                        {
                            closeFrame.ArrayView.Decode(Encoding.UTF8, offset + 2, length - 2, sb);
                        }
                    }

                    Debug.Assert(dataFrames.Count == 1); // control frame must not be fragmented !

                    Text = sb.ToString();
                    return;
                }
            }

            if (opCode == OpCode.Binary)
            {
                var array = new byte[dataFrames.Sum(f => f.ActualPayloadLength)];
                int copied = 0;
                for (var i = 0; i < dataFrames.Count; i++)
                {
                    var frame = dataFrames[i];
                    if (frame.HasMask) frame.DecodeMask();
                    copied += frame.Decode(array, copied);
                }

                Data = array;
                return;
            }

            using (var sb = StringBuilderShared.Acquire(dataFrames))
            {
                for (var i = 0; i < dataFrames.Count; i++)
                {
                    var frame = dataFrames[i];
                    if (frame.HasMask) frame.DecodeMask();
                    frame.Decode(sb);
                }

                Text = sb.ToString();
            }
        }

        public string Key { get; set; }

        public byte[] Data { get; set; }

        public string Text { get; set; }

        public short CloseStatusCode { get; private set; }
    }


}

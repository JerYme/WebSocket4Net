using System;
using System.Collections.Generic;
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

        public WebSocketDataFrame FirstDataFrames => _dataFrames?[0];
        public IList<WebSocketDataFrame> DataFrames => _dataFrames;

        public void Decode(int lengthExcluded)
        {
            if (_dataFrames == null) return;
            Decode(_dataFrames, lengthExcluded);
        }

        private void Decode(IList<WebSocketDataFrame> dataFrames, int lengthExcluded)
        {
            var firstFrame = dataFrames[0];
            var opCode = firstFrame.OpCode;
            Key = opCode.ToString();
            var length = firstFrame.ActualPayloadLength;
            var offset = firstFrame.ArrayView.Length - length - (lengthExcluded < 0 ? 0 : lengthExcluded);

            if (opCode == OpCode.Close)
            {
                if (firstFrame.HasMask)
                {
                    firstFrame.ArrayView.DecodeMask(firstFrame.MaskKey, offset, length);
                }

                using (var sb = StringBuilderShared.Acquire(firstFrame))
                {
                    if (length >= 2)
                    {
                        var closeStatusCode = firstFrame.ArrayView.ToArrayData(offset, 2);
                        CloseStatusCode = (short)(closeStatusCode[0] * 256 + closeStatusCode[1]);

                        if (length > 2)
                        {
                            firstFrame.ArrayView.Decode(Encoding.UTF8, offset + 2, length - 2, sb);
                        }
                    }
                    else if (length > 0)
                    {
                        firstFrame.ArrayView.Decode(Encoding.UTF8, offset, length, sb);
                    }

                    if (dataFrames.Count > 1)
                    {
                        for (var i = 1; i < dataFrames.Count; i++)
                        {
                            var frame = dataFrames[i];

                            offset = frame.ArrayView.Length - frame.ActualPayloadLength;
                            length = frame.ActualPayloadLength;

                            if (frame.HasMask)
                            {
                                frame.ArrayView.DecodeMask(frame.MaskKey, offset, length);
                            }

                            frame.ArrayView.Decode(Encoding.UTF8, offset, length, sb);
                        }
                    }

                    Text = sb.ToString();
                    return;
                }
            }

            if (opCode == OpCode.Binary)
            {
                var resultBuffer = new byte[dataFrames.Sum(f => f.ActualPayloadLength)];
                int copied = 0;
                for (var i = 0; i < dataFrames.Count; i++)
                {
                    var frame = dataFrames[i];

                    if (frame.HasMask)
                    {
                        frame.ArrayView.DecodeMask(frame.MaskKey, offset, length);
                    }

                    frame.ArrayView.CopyTo(resultBuffer, offset, copied, length);

                    copied += length;
                }

                Data = resultBuffer;
                return;
            }

            using (var sb = StringBuilderShared.Acquire(dataFrames))
            {
                for (var i = 0; i < dataFrames.Count; i++)
                {
                    var frame = dataFrames[i];

                    if (frame.HasMask)
                    {
                        frame.ArrayView.DecodeMask(frame.MaskKey, offset, length);
                    }

                    frame.ArrayView.Decode(Encoding.UTF8, offset, length, sb);
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

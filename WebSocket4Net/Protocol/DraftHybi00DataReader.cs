using System.Text;
using WebSocket4Net.Common;

namespace WebSocket4Net.Protocol
{
    class DraftHybi00DataReader : DataReaderBase
    {
        private byte? _type;
        private int _tempLength;
        private int? _length;

        private const byte _closingHandshakeType = 0xFF;

        public DraftHybi00DataReader(WebSocket websocket) : base(websocket)
        {
        }

        public DraftHybi00DataReader(WebSocket websocket, ArrayView<byte> arrayView) : base(websocket, arrayView)
        {
        }


        public override WebSocketFrameProcessed TryBuildWebSocketFrame(ArrayHolder<byte> ah, int offset, int length)
        {
            var webSocketFrameEx = BuildWebSocketFrameEx(ah.Array, offset, length);
            return new WebSocketFrameProcessed(webSocketFrameEx.Frame != null,null, webSocketFrameEx.Left);
        }

        WebSocketFrameEx BuildWebSocketFrameEx(byte[] readBuffer, int offset, int length)
        {
            int left = 0;

            var skipByteCount = 0;

            if (!_type.HasValue)
            {
                byte startByte = readBuffer[offset];
                skipByteCount = 1;
                _type = startByte;
            }

            //0xxxxxxx: Collect protocol data by end mark
            if ((_type.Value & 0x80) == 0x00)
            {
                byte lookForByte = 0xFF;

                int i;

                for (i = offset + skipByteCount; i < offset + length; i++)
                {
                    if (readBuffer[i] == lookForByte)
                    {
                        left = length - (i - offset + 1);

                        if (ArrayView.Length <= 0)
                        {
                            var commandInfo = new WebSocketFrame(OpCode.Text.ToString(), Encoding.UTF8.GetString(readBuffer, offset + skipByteCount, i - offset - skipByteCount));
                            Reset(false);
                            return new WebSocketFrameEx(commandInfo, left);
                        }
                        else
                        {
                            ArrayView.AddChunk(readBuffer, offset + skipByteCount, i - offset - skipByteCount, false);
                            var commandInfo = new WebSocketFrame(OpCode.Text.ToString(), ArrayView.Decode(Encoding.UTF8, 0, ArrayView.Length));
                            Reset(true);
                            return new WebSocketFrameEx(commandInfo, left);
                        }
                    }
                }

                base.AddChunk(readBuffer, offset + skipByteCount, length - skipByteCount, true);
                return new WebSocketFrameEx(left);
            }

            //10000000: Collect protocol data by length
            while (!_length.HasValue)
            {
                if (length <= skipByteCount)
                {
                    //No data to read
                    return new WebSocketFrameEx(left);
                }

                byte lengthByte = readBuffer[skipByteCount];
                //Closing handshake
                if (lengthByte == 0x00 && _type.Value == _closingHandshakeType)
                {
                    var commandInfo = new WebSocketFrame(OpCode.Close.ToString());
                    Reset(true);
                    return new WebSocketFrameEx(commandInfo, left);
                }

                int thisLength = (int)(lengthByte & 0x7F);
                _tempLength = _tempLength * 128 + thisLength;
                skipByteCount++;

                if ((lengthByte & 0x80) != 0x80)
                {
                    _length = _tempLength;
                    break;
                }
            }

            int requiredSize = _length.Value - ArrayView.Length;

            int leftSize = length - skipByteCount;

            if (leftSize < requiredSize)
            {
                AddChunk(readBuffer, skipByteCount, length - skipByteCount, true);
                return new WebSocketFrameEx(left);
            }

            left = leftSize - requiredSize;
            if (ArrayView.Length <= 0)
            {
                var commandInfo = new WebSocketFrame(OpCode.Text.ToString(), Encoding.UTF8.GetString(readBuffer, offset + skipByteCount, requiredSize));
                Reset(false);
                return new WebSocketFrameEx(commandInfo, left);
            }
            else
            {
                ArrayView.AddChunk(readBuffer, offset + skipByteCount, requiredSize, false);
                var commandInfo = new WebSocketFrame(ArrayView.Decode(Encoding.UTF8, 0, ArrayView.Length));
                Reset(true);
                return new WebSocketFrameEx(commandInfo, left);
            }
        }

        struct WebSocketFrameEx
        {
            public readonly WebSocketFrame Frame;
            public readonly int Left;

            public WebSocketFrameEx(int left) : this()
            {
                Left = left;
            }

            public WebSocketFrameEx(WebSocketFrame frame, int left)
            {
                Frame = frame;
                Left = left;
            }
        }


        void Reset(bool clearBuffer)
        {
            _type = null;
            _length = null;
            _tempLength = 0;

            if (clearBuffer) ArrayView.Clear();
        }


    }
}

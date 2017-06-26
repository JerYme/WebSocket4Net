using WebSocket4Net.Common;

namespace WebSocket4Net.Protocol
{
    public class WebSocketDataFrame
    {
        private int _actualPayloadLength = -1;
        public readonly ArrayView<byte> ArrayView;

        public WebSocketDataFrame(ArrayView<byte> arrayView)
        {
            ArrayView = arrayView;
        }

        public void Clear()
        {
            ArrayView.Clear();
            //ExtensionData = new byte[0];
            //ApplicationData = new byte[0];
            _actualPayloadLength = -1;
        }

        public bool IsControlFrame
        {
            get
            {
                sbyte opCode = OpCode;

                switch (opCode)
                {
                    case WebSocket4Net.OpCode.Ping:
                    case WebSocket4Net.OpCode.Pong:
                    case WebSocket4Net.OpCode.Close:
                        return true;
                }

                return false;
            }
        }

        public bool FIN => ((ArrayView[0] & 0x80) == 0x80);

        public bool RSV1 => ((ArrayView[0] & 0x40) == 0x40);

        public bool RSV2 => ((ArrayView[0] & 0x20) == 0x20);

        public bool RSV3 => ((ArrayView[0] & 0x10) == 0x10);

        public sbyte OpCode => (sbyte)(ArrayView[0] & 0x0f);

        public bool HasMask => ((ArrayView[1] & 0x80) == 0x80);

        public sbyte PayloadLength => (sbyte)(ArrayView[1] & 0x7f);


        public int ActualPayloadLength
        {
            get
            {
                if (_actualPayloadLength >= 0)
                    return _actualPayloadLength;

                var payloadLength = PayloadLength;

                if (payloadLength < 126)
                    _actualPayloadLength = payloadLength;
                else if (payloadLength == 126)
                {
                    _actualPayloadLength = ArrayView[2] * 256 + ArrayView[3];
                }
                else
                {
                    int len = 0;
                    int n = 1;

                    for (int i = 7; i >= 0; i--)
                    {
                        len += ArrayView[i + 2] * n;
                        n *= 256;
                    }

                    _actualPayloadLength = len;
                }

                return _actualPayloadLength;
            }
        }

        public byte[] MaskKey { get; set; }

        //public byte[] ExtensionData { get; set; }

        //public byte[] ApplicationData { get; set; }

        public int ArrayLength => ArrayView.Length;

    }
}

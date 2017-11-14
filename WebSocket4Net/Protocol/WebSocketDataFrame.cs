using System;
using System.Text;
using WebSocket4Net.Common;

namespace WebSocket4Net.Protocol
{
    public class WebSocketDataFrame
    {
        private int _actualPayloadLength = -1;
        public readonly ArrayView<byte> ArrayView;

        public WebSocketDataFrame(ArrayView<byte> arrayView)
        {
            if (arrayView == null) throw new ArgumentNullException(nameof(arrayView));
            ArrayView = arrayView;
        }

        public void Clear()
        {
            ArrayView.Clear();
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

        public int? ActualPayloadLengthNullable
        {
            get
            {
                if (_actualPayloadLength >= 0) return _actualPayloadLength;
                if (ArrayView.Length < 2) return null;

                var payloadLength = PayloadLength;
                if (payloadLength < 126) return payloadLength;
                if (payloadLength == 126)
                {
                    if (ArrayView.Length < 4) return null;
                    return ArrayView[2] * 256 + ArrayView[3];
                }

                if (ArrayView.Length < 10) return null;
                int len = 0;
                int n = 1;

                for (int i = 7; i >= 0; i--)
                {
                    len += ArrayView[i + 2] * n;
                    n *= 256;
                }

                return len;
            }
        }


        public int ActualPayloadLength
        {
            get
            {
                if (_actualPayloadLength >= 0) return _actualPayloadLength;

                var payloadLength = PayloadLength;

                if (payloadLength < 126)
                    _actualPayloadLength = payloadLength;
                else if (payloadLength == 126)
                    _actualPayloadLength = ArrayView[2] * 256 + ArrayView[3];
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

        public int PayloadIndex { get; set; } = -1;

        public byte[] MaskKey { get; set; }

        //public byte[] ExtensionData { get; set; }

        //public byte[] ApplicationData { get; set; }

        public int ArrayLength => ArrayView.Length;

        public void DecodeMask() => ArrayView.DecodeMask(MaskKey, PayloadIndex, ActualPayloadLength);

        public int Decode(StringBuilderShared sb) => ArrayView.Decode(Encoding.UTF8, PayloadIndex, ActualPayloadLength, sb);

        public int Decode(byte[] array, int index) => ArrayView.CopyTo(array, PayloadIndex, index, ActualPayloadLength);
    }
}

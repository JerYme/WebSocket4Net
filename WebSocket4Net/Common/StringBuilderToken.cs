using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebSocket4Net.Protocol;

namespace WebSocket4Net.Common
{
    public struct StringBuilderShared : IDisposable
    {
        [ThreadStatic]
        private static StringBuilder _sbStatic;
        [ThreadStatic]
        private static char[] _bufferStatic;

        public readonly StringBuilder StringBuilder;
        public readonly char[] Buffer;

        private StringBuilderShared(StringBuilder stringBuilder, char[] buffer)
        {
            StringBuilder = stringBuilder;
            Buffer = buffer;
        }

        public void Dispose() => StringBuilder.Length = 0;

        public static implicit operator StringBuilder(StringBuilderShared t) => t.StringBuilder;

        public override string ToString() => StringBuilder.ToString();

        internal static StringBuilderShared Acquire(IList<WebSocketDataFrame> frames) => Acquire(Encoding.UTF8.GetMaxCharCount(frames.Sum(x => x.ArrayView.Length)), Encoding.UTF8.GetMaxCharCount(MaxSegmentLength(frames)));

        private static int MaxSegmentLength(IList<WebSocketDataFrame> frames)
        {
            int max = int.MinValue;
            foreach (var f in frames)
            {
                var candidate = f.ArrayView.MaxChunkLength;
                if (candidate > max) max = candidate;
            }
            return max;
        }

        internal static StringBuilderShared Acquire(WebSocketDataFrame frame) => Acquire(Encoding.UTF8.GetMaxCharCount(frame.ActualPayloadLength), Encoding.UTF8.GetMaxCharCount(frame.ArrayView.MaxChunkLength));
        internal static StringBuilderShared Acquire(WebSocketDataFrame frame, int minusLength) => Acquire(Encoding.UTF8.GetMaxCharCount(frame.ActualPayloadLength - minusLength), Encoding.UTF8.GetMaxCharCount(frame.ArrayView.MaxChunkLength));

        internal static StringBuilderShared Acquire(int stringBuilderCapacity) => Acquire(stringBuilderCapacity, Encoding.UTF8.GetMaxCharCount(4096));

        internal static StringBuilderShared Acquire(int stringBuilderCapacity, int charBufferSize)
        {
            if (stringBuilderCapacity < 4) stringBuilderCapacity = 4;
            if (_sbStatic == null) _sbStatic = new StringBuilder(stringBuilderCapacity);
            else if (_sbStatic.Capacity < stringBuilderCapacity)
            {
                _sbStatic.EnsureCapacity(stringBuilderCapacity);
            }
            if (_bufferStatic == null || _bufferStatic.Length < charBufferSize) _bufferStatic = new char[charBufferSize];
            return new StringBuilderShared(_sbStatic, _bufferStatic);
        }
    }
}
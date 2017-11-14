using System;
using System.Text;

namespace WebSocket4Net.Common
{
    public static class ArrayViewExtension
    {
        //public static string Decode(this ArraySegmentList<byte> arraySegmentList, Encoding encoding) => Decode(arraySegmentList, encoding, 0, arraySegmentList.TotalLength);

        public static string Decode(this ArrayView<byte> arrayView) => Decode(arrayView, Encoding.UTF8, 0, arrayView.Length);
        public static string Decode(this ArrayView<byte> arrayView, Encoding encoding) => Decode(arrayView, encoding, 0, arrayView.Length);

        public static string Decode(this ArrayView<byte> arrayView, Encoding encoding, int offset, int length)
        {
            if (length == 0) return string.Empty;

            if (arrayView.Length == 0) return string.Empty;

            var sb = new StringBuilder(encoding.GetMaxCharCount(arrayView.Length));
            var size = Decode(arrayView, encoding, offset, length, sb);
            return sb.ToString(0, size);
        }

        public static int Decode(this ArrayView<byte> arrayView, Encoding encoding, int offset, int length, StringBuilder sb)
        {
            if (length == 0) return 0;
            if (arrayView.Length == 0) return 0;
            var charsBuffer = new char[encoding.GetMaxCharCount(arrayView.MaxChunkLength)];
            return Decode(arrayView, encoding, offset, length, sb, charsBuffer);
        }

        public static int Decode(this ArrayView<byte> arrayView, Encoding encoding, int offset, int length, StringBuilderShared sbt)
        {
            if (length == 0) return 0;
            if (arrayView.Length == 0) return 0;
            var charsBuffer = sbt.Buffer ?? new char[encoding.GetMaxCharCount(arrayView.MaxChunkLength)];
            return Decode(arrayView, encoding, offset, length, sbt.StringBuilder, charsBuffer);
        }

        public static int Decode(this ArrayView<byte> arrayView, Encoding encoding, int offset, int length, StringBuilder sb, char[] charsBuffer)
        {
            if (length == 0) return 0;
            if (arrayView.Length == 0) return 0;

            int totalBytes = 0;
            int totalChars = 0;

            int startChunkIndex = 0;

            if (offset > 0)
            {
                arrayView.BinarySearchInternal(offset, out startChunkIndex);
            }

            var decoder = encoding.GetDecoder();
            foreach (var chunk in arrayView.Range(startChunkIndex))
            {
                int decodeOffset = chunk.Offset;
                int byteLengthToDecode = Math.Min(length - totalBytes, chunk.Length);

                if (chunk.Index == startChunkIndex && offset > 0)
                {
                    decodeOffset = offset - chunk.StartIndex + chunk.Offset;
                    byteLengthToDecode = Math.Min(chunk.Length - offset + chunk.StartIndex, byteLengthToDecode);
                }

                //decoder.Convert(segment.Array, decodeOffset, toBeDecoded, charsBuffer, totalChars, charsBuffer.Length - totalChars, flush, out bytesUsed, out charsUsed, out completed);
                int bytesUsed;
                int charsUsed;
                bool completed;
                decoder.Convert(chunk.Array, decodeOffset, byteLengthToDecode, charsBuffer, 0, charsBuffer.Length, chunk.Index == arrayView.ChunkCount - 1, out bytesUsed, out charsUsed, out completed);
                sb.Append(charsBuffer, 0, charsUsed);
                totalChars += charsUsed;
                totalBytes += bytesUsed;

                if (totalBytes >= length) break;
            }
            return totalChars;
        }

        internal static string Decode(this ArrayChunk<byte> chunk)
        {
            StringBuilder sb = new StringBuilder();
            char[] charsBuffer = new char[chunk.Length];

            int bytesUsed;
            int charsUsed;
            bool completed;
            Encoding.UTF8.GetDecoder().Convert(chunk.Array, chunk.Offset, chunk.Length, charsBuffer, 0, charsBuffer.Length, true, out bytesUsed, out charsUsed, out completed);
            sb.Append(charsBuffer, 0, charsUsed);
            return sb.ToString();
        }


        public static void DecodeMask(this ArrayView<byte> arrayView, byte[] mask, int offset, int length)
        {
            var maskLen = mask.Length;
            int startSegmentIndex;
            var startSegment = arrayView.BinarySearchInternal(offset, out startSegmentIndex);

            var shouldDecode = Math.Min(length, startSegment.Length - offset + startSegment.StartIndex);
            var from = offset - startSegment.StartIndex + startSegment.Offset;

            var index = 0;

            for (var i = from; i < from + shouldDecode; i++)
            {
                startSegment.Array[i] = (byte)(startSegment.Array[i] ^ mask[index++ % maskLen]);
            }

            if (index >= length) return;

            foreach (var segment in arrayView.Range(startSegmentIndex + 1))
            {
                shouldDecode = Math.Min(length - index, segment.Length);

                for (var j = segment.Offset; j < segment.Offset + shouldDecode; j++)
                {
                    segment.Array[j] = (byte)(segment.Array[j] ^ mask[index++ % maskLen]);
                }

                if (index >= length)
                    return;
            }
        }
    }
}
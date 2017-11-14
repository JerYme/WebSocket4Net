using System;
using System.Collections.Generic;
using System.Linq;
using SuperSocket.ClientEngine;

namespace WebSocket4Net.Common
{
    public class ArrayView<T> where T : IEquatable<T>
    {
        private readonly List<ArrayChunk<T>> _chunks;

        private ArrayChunk<T> _quickSearchChunk;

        private int _length;

        public ArrayView()
        {
            _chunks = new List<ArrayChunk<T>>();
        }

        public T this[int index]
        {
            get
            {
                ArrayChunk<T> segment;
                var internalIndex = GetInternalIndex(index, out segment);
                if (internalIndex < 0) throw new IndexOutOfRangeException();

                return segment.Array[internalIndex];
            }
        }

        public IEnumerable<ArrayChunk<T>> Range(int start) => Range(start, _chunks.Count - start);
        public IEnumerable<ArrayChunk<T>> Range(int start, int count)
        {
            for (int i = start; i < count && i > -1 && i < _chunks.Count; i++)
            {
                var arrayChunk = _chunks[i];
                arrayChunk.Index = i;
                yield return arrayChunk;
            }
        }

        private int GetInternalIndex(int index, out ArrayChunk<T> chunk)
        {
            if (_quickSearchChunk != null)
            {
                if (index >= _quickSearchChunk.StartIndex && index <= _quickSearchChunk.EndIndex)
                {
                    chunk = _quickSearchChunk;
                    return index + chunk.Offset - chunk.StartIndex;
                }
            }

            chunk = BinarySearchInternal(index);
            if (chunk == null) return -1;
            _quickSearchChunk = chunk;

            return index + chunk.Offset - chunk.StartIndex;
        }

        private ArrayChunk<T> BinarySearchInternal(int index)
        {
            if (index < 0 || index >= Length) return null;

            var i = _chunks.BinarySearch(new ArrayChunk<T>(index));
            if (i > 0) return _chunks[i];
            i = ~i;
            var chunk = i == _chunks.Count
                ? _chunks[_chunks.Count - 1]
                : i == 0
                    ? _chunks[0]
                    : _chunks[i - 1];
            return Match(chunk, index);
        }

        internal ArrayChunk<T> BinarySearchInternal(int index, out int chunkIndex)
        {
            if (index < 0 || index >= Length)
            {
                chunkIndex = -1;
                return null;
            }

            var i = _chunks.BinarySearch(new ArrayChunk<T>(index), Comparer<ArrayChunk<T>>.Default);
            if (i > 0)
            {
                chunkIndex = i;
                return _chunks[i];
            }
            i = ~i;

            if (i == _chunks.Count)
            {
                var match = Match(_chunks[_chunks.Count - 1], index);
                chunkIndex = match != null ? _chunks.Count - 1 : -1;
                return match;
            }

            if (i == 0)
            {
                var match = Match(_chunks[0], index);
                chunkIndex = match != null ? 0 : -1;
                return match;
            }

            {
                var match = Match(_chunks[i - 1], index);
                chunkIndex = match != null ? i - 1 : -1;
                return match;
            }
        }

        private static ArrayChunk<T> Match(ArrayChunk<T> chunk, int index) => chunk.StartIndex <= index && index <= chunk.EndIndex ? chunk : null;

        public void CopyTo(T[] array, int arrayIndex) => CopyTo(array, 0, arrayIndex, Math.Min(array.Length, Length - arrayIndex));

        public int Length => _length;

        public void RemoveChunkAt(int index)
        {
            var chunk = _chunks[index];
            int length = chunk.Length;

            _chunks.RemoveAt(index);

            _quickSearchChunk = null;

            //the removed item is not the the last item 
            if (index != _chunks.Count)
            {
                for (int i = index; i < _chunks.Count; i++)
                {
                    _chunks[i].Move(-length);
                }
            }

            _length -= length;
        }

        public ArrayChunk<T> AddChunk(T[] array, int offset, int length, bool copy) => AddChunk(ArrayChunk<T>.New(array, offset, length, _length, copy));

        public ArrayChunk<T> AddChunk(ArrayChunk<T> chunk)
        {
            if (chunk == null) return null;
            _length += chunk.Length;
            _chunks.Add(chunk);
            return chunk;
        }

        public int ChunkCount => _chunks.Count;
        public int MaxChunkLength
        {
            get
            {
                int max = int.MinValue;
                foreach (var c in _chunks)
                {
                    if (c.Length > max) max = c.Length;
                }
                return max;
            }
        }

        public void Clear()
        {
            _chunks.Clear();
            _quickSearchChunk = null;
            _length = 0;
        }

        public void ClearBuffers()
        {
            foreach (var chunk in _chunks)
            {
                chunk.ClearBuffer();
            }
        }

        public T[] ToArrayData() => ToArrayData(0, _length);

        public T[] ToArrayData(int startIndex, int length)
        {
            var result = new T[length];
            int from = 0, total = 0;

            var startSegmentIndex = 0;

            if (startIndex != 0)
            {
                var startSegment = BinarySearchInternal(startIndex, out startSegmentIndex);
                if (startSegment == null) throw new IndexOutOfRangeException();
                from = startIndex - startSegment.StartIndex;
            }

            for (var i = startSegmentIndex; i < _chunks.Count; i++)
            {
                var currentSegment = _chunks[i];
                var len = Math.Min(currentSegment.Length - @from, length - total);
                Array.Copy(currentSegment.Array, currentSegment.Offset + from, result, total, len);
                total += len;

                if (total >= length)
                    break;

                from = 0;
            }

            return result;
        }

        public void TrimEnd(int trimSize)
        {
            if (trimSize <= 0) return;

            int newEndIndex = Length - trimSize - 1;

            for (int i = _chunks.Count - 1; i >= 0; i--)
            {
                var s = _chunks[i];

                if (s.StartIndex > newEndIndex || newEndIndex >= s.EndIndex)
                {
                    RemoveChunkAt(i);
                    continue;
                }

                s.Length = s.Length - trimSize;
                _length -= trimSize;
                return;
            }
        }

        public int SearchLastSegment(SearchMarkState<T> state)
        {
            if (_chunks.Count <= 0) return -1;

            var lastSegment = _chunks[_chunks.Count - 1];

            if (lastSegment == null) return -1;

            var result = lastSegment.Array.SearchMark(lastSegment.Offset, lastSegment.Length, state.Mark);

            if (!result.HasValue) return -1;

            if (result.Value > 0)
            {
                state.Matched = 0;
                return result.Value - lastSegment.Offset + lastSegment.StartIndex;
            }

            state.Matched = 0 - result.Value;
            return -1;
        }

        public int CopyTo(T[] to) => CopyTo(to, 0, 0, Math.Min(_length, to.Length));

        public int CopyTo(T[] to, int srcIndex, int toIndex, int length)
        {
            int copied = 0;

            int offsetSegmentIndex;
            ArrayChunk<T> offsetSegment;

            if (srcIndex > 0)
                offsetSegment = BinarySearchInternal(srcIndex, out offsetSegmentIndex);
            else
            {
                offsetSegment = _chunks[0];
                offsetSegmentIndex = 0;
            }

            int thisOffset = srcIndex - offsetSegment.StartIndex + offsetSegment.Offset;
            int thisCopied = Math.Min(offsetSegment.Length - thisOffset + offsetSegment.Offset, length - copied);

            Array.Copy(offsetSegment.Array, thisOffset, to, copied + toIndex, thisCopied);

            copied += thisCopied;

            if (copied >= length) return copied;

            for (var i = offsetSegmentIndex + 1; i < _chunks.Count; i++)
            {
                var segment = _chunks[i];
                thisCopied = Math.Min(segment.Length, length - copied);
                Array.Copy(segment.Array, segment.Offset, to, copied + toIndex, thisCopied);
                copied += thisCopied;

                if (copied >= length) break;
            }

            return copied;
        }
    }
}

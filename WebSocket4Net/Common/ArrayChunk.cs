using System;
using SuperSocket.ClientEngine;

namespace WebSocket4Net.Common
{
    public class ArrayChunk<T> : IComparable, IComparable<ArrayChunk<T>>
    {
        private T[] _array;
        private ArrayHolder<T> _arrayHolder;

        public ArrayChunk(int startIndex)
        {
            StartIndex = startIndex;
        }

        public ArrayChunk(ArrayHolder<T> arrayHolder, int offset, int length, int startIndex)
        {
            _arrayHolder = arrayHolder;
            Offset = offset;
            Length = length;
            StartIndex = startIndex;
        }

        public ArrayChunk(T[] array, int offset, int length, int startIndex)
        {
            _array = array;
            Offset = offset;
            Length = length;
            StartIndex = startIndex;
        }

        public static ArrayChunk<T> New(ArrayHolder<T> ah, int offset, int length, int startIndex)
        {
            return length <= 0 ? null : new ArrayChunk<T>(ah, offset, length, startIndex);
        }

        public static ArrayChunk<T> New(T[] array, int offset, int length, int startIndex, bool toBeCopied)
        {
            if (length <= 0) return null;

            return !toBeCopied
                ? new ArrayChunk<T>(array, offset, length, startIndex)
                : new ArrayChunk<T>(array?.CloneRange(offset, length), 0, length, startIndex);
        }


        /// <summary>
        /// Gets the array.
        /// </summary>
        public T[] Array => _array ?? _arrayHolder?.Array;

        /// <summary>
        /// Gets the count.
        /// </summary>
        public int Length;


        /// <summary>
        /// Gets the offset.
        /// </summary>
        public int Offset;

        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>
        /// The index.
        /// </value>
        public int StartIndex;

        /// <summary>
        /// Gets the end index.
        /// </summary>
        /// <value>
        /// The end index.
        /// </value>
        public int EndIndex => StartIndex + Length - 1;

        /// <summary>
        /// Moves the specified shift index.
        /// </summary>
        /// <param name="shiftIndex">Index of the shift.</param>
        public void Move(int shiftIndex) => StartIndex += shiftIndex;

        /// <summary>
        /// Copies the buffer.
        /// </summary>
        public void CopyBuffer()
        {
            if (_array != null)
            {
                _array = _array.CloneRange(Offset, Length);
                Offset = 0;
            }
            else
            {
                _arrayHolder?.CopyBuffer();
            }
        }

        /// <summary>
        /// Clears the buffer.
        /// </summary>
        public void ClearBuffer()
        {
            _array = null;
            _arrayHolder = null;
        }

        int IComparable.CompareTo(object obj) => StartIndex.CompareTo(((ArrayChunk<T>)obj).StartIndex);

        int IComparable<ArrayChunk<T>>.CompareTo(ArrayChunk<T> other) => StartIndex.CompareTo(other.StartIndex);

    }
}

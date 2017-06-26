namespace WebSocket4Net.Common
{
    public class ArrayHolder<T>
    {
        public ArrayHolder()
        {
        }

        public ArrayHolder(T[] array)
        {
            Array = array;
        }

        public T[] Array;

        private bool _isCopy;

        /// <summary>
        /// Copies the buffer.
        /// </summary>
        public void CopyBuffer()
        {
            if (_isCopy) return;
            var array = Array;
            if (array == null) return;
            var copy = new T[array.Length];
            array.CopyTo(copy, 0);
            Array = copy;
            _isCopy = true;
        }
    }
}
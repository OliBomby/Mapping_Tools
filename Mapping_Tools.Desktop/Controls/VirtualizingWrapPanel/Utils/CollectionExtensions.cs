using System.Collections;

namespace Mapping_Tools.Desktop.Controls.VirtualizingWrapPanel.Utils;

internal static class CollectionExtensions
{
    internal static void InsertMany<T>(this List<T> list, int index, T item, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (count == 0)
            return;

        list.InsertRange(index, new RepeatCollection<T>(item, count));
    }

    private sealed class RepeatCollection<T> : ICollection<T>
    {
        private readonly T repeatedItem;

        public RepeatCollection(T repeatedItem, int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            this.repeatedItem = repeatedItem;
            Count = count;
        }

        public int Count { get; }
        public bool IsReadOnly => true;

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(T item)
        {
            throw new NotSupportedException();
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return repeatedItem;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));

            if ((uint)arrayIndex > (uint)array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("Destination array is not long enough.", nameof(array));

            int end = arrayIndex + Count;
            for (int i = arrayIndex; i < end; i++)
                array[i] = repeatedItem;
        }
    }
}

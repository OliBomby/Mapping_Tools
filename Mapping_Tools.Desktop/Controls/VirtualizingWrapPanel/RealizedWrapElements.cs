using Avalonia;
using Avalonia.Controls;
using Mapping_Tools.Desktop.Controls.VirtualizingWrapPanel.Utils;

namespace Mapping_Tools.Desktop.Controls.VirtualizingWrapPanel;

/// <summary>
/// Stores the realized element state for a virtualizing panel that arranges its children
/// in a wrap layout, such as <see cref="VirtualizingWrapPanel"/>.
/// </summary>
internal class RealizedWrapElements
{
    private int firstIndex;
    private readonly List<Control?> elements;
    private readonly List<Size> sizes;
    private readonly Dictionary<Control, int> elementToIndex = new();

    public RealizedWrapElements()
    {
        // Pre-allocate with reasonable capacity to reduce reallocations
        elements = new List<Control?>(32);
        sizes = new List<Size>(32);
    }

    /// <summary>
    /// Gets the number of realized elements.
    /// </summary>
    public int Count => elements.Count;

    /// <summary>
    /// Gets the index of the first realized element, or -1 if no elements are realized.
    /// </summary>
    public int FirstIndex => elements.Count > 0 ? firstIndex : -1;

    /// <summary>
    /// Gets the index of the last realized element, or -1 if no elements are realized.
    /// </summary>
    public int LastIndex => elements.Count > 0 ? firstIndex + elements.Count - 1 : -1;

    /// <summary>
    /// Gets the elements.
    /// </summary>
    public IReadOnlyList<Control?> Elements => elements;

    /// <summary>
    /// Gets the sizes of the elements on the primary axis.
    /// </summary>
    public IReadOnlyList<Size> Sizes => sizes;

    /// <summary>
    /// Adds a newly realized element to the collection.
    /// </summary>
    /// <param name="index">The index of the element.</param>
    /// <param name="element">The element.</param>
    /// <param name="size">The size of the element on the primary axis.</param>
    public void Add(int index, Control element, Size size)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        int count = elements.Count;

        if (count == 0)
        {
            elements.Add(element);
            sizes.Add(size);
            elementToIndex[element] = index;
            firstIndex = index;
        }
        else if (index == firstIndex + count)
        {
            elements.Add(element);
            sizes.Add(size);
            elementToIndex[element] = index;
        }
        else if (index == firstIndex - 1)
        {
            --firstIndex;
            elements.Insert(0, element);
            sizes.Insert(0, size);
            elementToIndex[element] = index;
        }
        else
        {
            throw new NotSupportedException("Can only add items to the beginning or end of realized elements.");
        }
    }

    /// <summary>
    /// Gets the element at the specified index, if realized.
    /// </summary>
    /// <param name="index">The index in the source collection of the element to get.</param>
    /// <returns>The element if realized; otherwise null.</returns>
    public Control? GetElement(int index)
    {
        int i = index - firstIndex;
        int count = elements.Count;
        if (i >= 0 && i < count)
            return elements[i];
        return null;
    }

    /// <summary>
    /// Gets the Size of the element, if realized.
    /// </summary>
    /// <returns>
    /// The size of the element or Infinite if not found
    /// </returns>
    public Size? GetElementSize(Control? child)
    {
        if (child == null) return null;

        int index = GetIndex(child);

        if (index < 0)
            return null;

        int localIndex = index - firstIndex;
        if (localIndex < 0 || localIndex >= sizes.Count)
            return null;

        return sizes[localIndex];
    }

    /// <summary>
    /// Gets the Size of the element, if realized.
    /// </summary>
    /// <param name="index">The index to lookup</param>
    /// <returns>The size of the element or null if not found</returns>
    public Size? GetElementSize(int index)
    {
        if (index < FirstIndex)
            return null;

        int localIndex = index - firstIndex;
        if (localIndex >= sizes.Count)
            return null;

        return sizes[localIndex];
    }

    /// <summary>
    /// Gets the index of the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The index or -1 if the element is not present in the collection.</returns>
    public int GetIndex(Control element)
    {
        return elementToIndex.TryGetValue(element, out int index) ? index : -1;
    }

    /// <summary>
    /// Updates the elements in response to items being inserted into the source collection.
    /// </summary>
    /// <param name="index">The index in the source collection of the insert.</param>
    /// <param name="count">The number of items inserted.</param>
    /// <param name="updateElementIndex">A method used to update the element indexes.</param>
    public void ItemsInserted(int index, int count, Action<Control, int, int> updateElementIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        int elementCount = elements.Count;
        if (elementCount == 0)
            return;

        // Get the index within the realized _elements collection.
        int first = firstIndex;
        int realizedIndex = index - first;

        if (realizedIndex < elementCount)
        {
            // The insertion point affects the realized elements. Update the index of the
            // elements after the insertion point.
            int start = Math.Max(realizedIndex, 0);

            for (int i = start; i < elementCount; ++i)
            {
                if (elements[i] is not { } element)
                    continue;
                int oldIndex = i + first;
                int newIndex = oldIndex + count;
                updateElementIndex(element, oldIndex, newIndex);
                elementToIndex[element] = newIndex;
            }

            if (realizedIndex < 0)
            {
                // The insertion point was before the first element, update the first index.
                firstIndex += count;
            }
            else
            {
                // The insertion point was within the realized elements, insert an empty space
                // in _elements and _sizes.
                elements.InsertMany(realizedIndex, null, count);
                sizes.InsertMany(realizedIndex, Size.Infinity, count);
            }
        }
    }

    /// <summary>
    /// Updates the elements in response to items being removed from the source collection.
    /// </summary>
    /// <param name="index">The index in the source collection of the remove.</param>
    /// <param name="count">The number of items removed.</param>
    /// <param name="updateElementIndex">A method used to update the element indexes.</param>
    /// <param name="recycleElement">A method used to recycle elements.</param>
    public void ItemsRemoved(
        int index,
        int count,
        Action<Control, int, int> updateElementIndex,
        Action<Control> recycleElement)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        int elementCount = elements.Count;
        if (elementCount == 0)
            return;

        // Get the removal start and end index within the realized _elements collection.
        int first = firstIndex;
        int startIndex = index - first;
        int endIndex = index + count - first;

        if (endIndex < 0)
        {
            // The removed range was before the realized elements. Update the first index and
            // the indexes of the realized elements.
            firstIndex -= count;

            int newIndex = firstIndex;
            for (int i = 0; i < elementCount; ++i)
            {
                if (elements[i] is { } element)
                {
                    updateElementIndex(element, newIndex + count, newIndex);
                    elementToIndex[element] = newIndex;
                }

                ++newIndex;
            }
        }
        else if (startIndex < elementCount)
        {
            // Recycle and remove the affected elements.
            int start = Math.Max(startIndex, 0);
            int end = Math.Min(endIndex, elementCount);

            for (int i = start; i < end; ++i)
            {
                if (elements[i] is { } element)
                {
                    elements[i] = null;
                    elementToIndex.Remove(element);
                    recycleElement(element);
                }
            }

            elements.RemoveRange(start, end - start);
            sizes.RemoveRange(start, end - start);

            // If the remove started before and ended within our realized elements, then our new
            // first index will be the index where the remove started. Mark StartU as unstable
            // because we can't rely on it now to estimate element heights.
            if (startIndex <= 0 && end < elementCount)
            {
                firstIndex = first = index;
            }

            // Update the indexes of the elements after the removed range.
            end = elements.Count;
            int newIndex = first + start;
            for (int i = start; i < end; ++i)
            {
                if (elements[i] is { } element)
                {
                    updateElementIndex(element, newIndex + count, newIndex);
                    elementToIndex[element] = newIndex;
                }

                ++newIndex;
            }
        }
    }

    /// <summary>
    /// Updates the elements in response to items being replaced in the source collection.
    /// </summary>
    /// <param name="index">The index in the source collection of the remove.</param>
    /// <param name="count">The number of items removed.</param>
    /// <param name="recycleElement">A method used to recycle elements.</param>
    public void ItemsReplaced(int index, int count, Action<Control> recycleElement)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        int elementCount = elements.Count;
        if (elementCount == 0)
            return;

        // Get the index within the realized _elements collection.
        int startIndex = index - firstIndex;
        int endIndex = Math.Min(startIndex + count, elementCount);

        if (startIndex >= 0 && endIndex > startIndex)
        {
            for (int i = startIndex; i < endIndex; ++i)
            {
                if (elements[i] is { } element)
                {
                    recycleElement(element);
                    elementToIndex.Remove(element);
                    elements[i] = null;
                    sizes[i] = Size.Infinity;
                }
            }
        }
    }

    /// <summary>
    /// Recycles all elements in response to the source collection being reset.
    /// </summary>
    /// <param name="recycleElement">A method used to recycle elements.</param>
    public void ItemsReset(Action<Control> recycleElement)
    {
        int count = elements.Count;
        if (count == 0)
            return;

        for (int i = 0; i < count; i++)
        {
            if (elements[i] is { } e)
            {
                elements[i] = null;
                elementToIndex.Remove(e);
                recycleElement(e);
            }
        }

        elements.Clear();
        sizes.Clear();
        elementToIndex.Clear();
    }

    /// <summary>
    /// Recycles elements before a specific index.
    /// </summary>
    /// <param name="index">The index in the source collection of new first element.</param>
    /// <param name="recycleElement">A method used to recycle elements.</param>
    public void RecycleElementsBefore(int index, Action<Control, int> recycleElement)
    {
        int count = elements.Count;
        int first = firstIndex;

        if (index <= first || count == 0)
            return;

        if (index > first + count - 1)
        {
            RecycleAllElements(recycleElement);
        }
        else
        {
            int endIndex = index - first;

            for (int i = 0; i < endIndex; ++i)
            {
                if (elements[i] is { } e)
                {
                    elements[i] = null;
                    elementToIndex.Remove(e);
                    recycleElement(e, i + first);
                }
            }

            elements.RemoveRange(0, endIndex);
            sizes.RemoveRange(0, endIndex);
            firstIndex = index;
        }
    }

    /// <summary>
    /// Recycles elements after a specific index.
    /// </summary>
    /// <param name="index">The index in the source collection of new last element.</param>
    /// <param name="recycleElement">A method used to recycle elements.</param>
    public void RecycleElementsAfter(int index, Action<Control, int> recycleElement)
    {
        int count = elements.Count;
        int first = firstIndex;

        if (index >= first + count - 1 || count == 0)
            return;

        if (index < first)
        {
            RecycleAllElements(recycleElement);
        }
        else
        {
            int startIndex = index + 1 - first;

            for (int i = startIndex; i < count; ++i)
            {
                if (elements[i] is { } e)
                {
                    elements[i] = null;
                    elementToIndex.Remove(e);
                    recycleElement(e, i + first);
                }
            }

            int removeCount = count - startIndex;
            elements.RemoveRange(startIndex, removeCount);
            sizes.RemoveRange(startIndex, removeCount);
        }
    }

    /// <summary>
    /// Recycles all realized elements.
    /// </summary>
    /// <param name="recycleElement">A method used to recycle elements.</param>
    public void RecycleAllElements(Action<Control, int> recycleElement)
    {
        int count = elements.Count;
        if (count == 0)
            return;

        int first = firstIndex;
        for (int i = 0; i < count; i++)
        {
            if (elements[i] is { } e)
            {
                elements[i] = null;
                elementToIndex.Remove(e);
                recycleElement(e, i + first);
            }
        }

        firstIndex = 0;
        elements.Clear();
        sizes.Clear();
        elementToIndex.Clear();
    }

    /// <summary>
    /// Resets the element list and prepares it for reuse.
    /// </summary>
    public void ResetForReuse()
    {
        firstIndex = 0;
        elements.Clear();
        sizes.Clear();
        elementToIndex.Clear();
    }
}

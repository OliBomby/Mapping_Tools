using System.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Mapping_Tools.Desktop.Controls.VirtualizingWrapPanel.Utils;

internal class VirtualizingWrapPanelSnapPointList : IReadOnlyList<double>
{
    private const int extra_count = 2;
    private readonly Orientation orientation;
    private readonly Orientation parentOrientation;
    private readonly RealizedWrapElements realizedElements;
    private readonly double size;
    private readonly SnapPointsAlignment snapPointsAlignment;
    private readonly int start = -1;

    public VirtualizingWrapPanelSnapPointList(RealizedWrapElements realizedElements, int count, Orientation orientation, Orientation parentOrientation,
        SnapPointsAlignment snapPointsAlignment, double size)
    {
        this.realizedElements = realizedElements;
        this.orientation = orientation;
        this.parentOrientation = parentOrientation;
        this.snapPointsAlignment = snapPointsAlignment;
        this.size = size;
        if (parentOrientation == orientation)
        {
            start = Math.Max(0, this.realizedElements.FirstIndex - extra_count);
            Count = Math.Min(count - 1, this.realizedElements.LastIndex + extra_count);
        }
    }

    public double this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            index += start;

            double snapPoint = 0;
            double averageElementSize = size;

            Control? container;
            switch (orientation)
            {
                case Orientation.Horizontal:
                    container = realizedElements.GetElement(index);
                    if (container != null)
                    {
                        switch (snapPointsAlignment)
                        {
                            case SnapPointsAlignment.Near:
                                snapPoint = container.Bounds.Left;
                                break;
                            case SnapPointsAlignment.Center:
                                snapPoint = container.Bounds.Center.X;
                                break;
                            case SnapPointsAlignment.Far:
                                snapPoint = container.Bounds.Right;
                                break;
                        }
                    }
                    else if (index < realizedElements.FirstIndex)
                    {
                        // Estimate position by stepping backward from the first realized element.
                        var firstElement = realizedElements.GetElement(realizedElements.FirstIndex);
                        double basePosition = firstElement != null
                            ? firstElement.Bounds.Left
                            : realizedElements.FirstIndex * averageElementSize;
                        int stepsBack = realizedElements.FirstIndex - index;
                        snapPoint = basePosition - stepsBack * averageElementSize;
                        switch (snapPointsAlignment)
                        {
                            case SnapPointsAlignment.Center:
                                snapPoint += averageElementSize / 2;
                                break;
                            case SnapPointsAlignment.Far:
                                snapPoint += averageElementSize;
                                break;
                        }
                    }
                    else
                    {
                        // index > LastIndex: estimate forward from the last realized element.
                        int stepsForward = index - realizedElements.LastIndex;
                        var lastElement = realizedElements.GetElement(realizedElements.LastIndex);
                        double basePosition = lastElement != null
                            ? lastElement.Bounds.Right
                            : (realizedElements.LastIndex + 1) * averageElementSize;
                        snapPoint = basePosition + (stepsForward - 1) * averageElementSize;
                        switch (snapPointsAlignment)
                        {
                            case SnapPointsAlignment.Center:
                                snapPoint += averageElementSize / 2;
                                break;
                            case SnapPointsAlignment.Far:
                                snapPoint += averageElementSize;
                                break;
                        }
                    }

                    break;
                case Orientation.Vertical:
                    container = realizedElements.GetElement(index);
                    if (container != null)
                    {
                        switch (snapPointsAlignment)
                        {
                            case SnapPointsAlignment.Near:
                                snapPoint = container.Bounds.Top;
                                break;
                            case SnapPointsAlignment.Center:
                                snapPoint = container.Bounds.Center.Y;
                                break;
                            case SnapPointsAlignment.Far:
                                snapPoint = container.Bounds.Bottom;
                                break;
                        }
                    }
                    else if (index < realizedElements.FirstIndex)
                    {
                        // Estimate position by stepping backward from the first realized element.
                        var firstElement = realizedElements.GetElement(realizedElements.FirstIndex);
                        double basePosition = firstElement != null
                            ? firstElement.Bounds.Top
                            : realizedElements.FirstIndex * averageElementSize;
                        int stepsBack = realizedElements.FirstIndex - index;
                        snapPoint = basePosition - stepsBack * averageElementSize;
                        switch (snapPointsAlignment)
                        {
                            case SnapPointsAlignment.Center:
                                snapPoint += averageElementSize / 2;
                                break;
                            case SnapPointsAlignment.Far:
                                snapPoint += averageElementSize;
                                break;
                        }
                    }
                    else
                    {
                        // index > LastIndex: estimate forward from the last realized element.
                        int stepsForward = index - realizedElements.LastIndex;
                        var lastElement = realizedElements.GetElement(realizedElements.LastIndex);
                        double basePosition = lastElement != null
                            ? lastElement.Bounds.Bottom
                            : (realizedElements.LastIndex + 1) * averageElementSize;
                        snapPoint = basePosition + (stepsForward - 1) * averageElementSize;
                        switch (snapPointsAlignment)
                        {
                            case SnapPointsAlignment.Center:
                                snapPoint += averageElementSize / 2;
                                break;
                            case SnapPointsAlignment.Far:
                                snapPoint += averageElementSize;
                                break;
                        }
                    }

                    break;
            }

            return snapPoint;
        }
    }

    public int Count => parentOrientation != orientation ? 0 : field - start + 1;

    public IEnumerator<double> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

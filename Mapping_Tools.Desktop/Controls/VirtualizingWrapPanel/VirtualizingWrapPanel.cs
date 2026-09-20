using System.Collections.Specialized;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mapping_Tools.Desktop.Controls.VirtualizingWrapPanel.Utils;

namespace Mapping_Tools.Desktop.Controls.VirtualizingWrapPanel;

/// <summary>
///     An implementation of a wrap panel that supports virtualization and can be used in horizontal and vertical
///     orientation.
/// </summary>
public class VirtualizingWrapPanel : VirtualizingPanel, IScrollSnapPointsInfo, IItemSizeProvider
{
    private const double epsilon = 0.001;
    private const int row_cache_capacity = 256; // "some rows" to improve performance without excessive memory

    private const int recycle_pool_max_size = 32; // max containers kept per recycle key to bound visual-tree size

    // The fallback size in case size calculation went wrong
    private static readonly Size fallbackItemSize = new(48, 48);

    /// <summary>
    ///     Gets an empty size
    /// </summary>
    private static readonly Size emptySize = new(0, 0);

    /// <summary>
    ///     Defines the <see cref="Orientation" /> property.
    /// </summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        WrapPanel.OrientationProperty.AddOwner<VirtualizingWrapPanel>(
            new StyledPropertyMetadata<Orientation>(Orientation.Horizontal));

    /// <summary>
    ///     Defines the <see cref="ItemSize" /> property.
    /// </summary>
    public static readonly StyledProperty<Size> ItemSizeProperty =
        AvaloniaProperty.Register<VirtualizingWrapPanel, Size>(nameof(ItemSize), emptySize);

    /// <summary>
    ///     Defines the <see cref="AllowDifferentSizedItems" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> AllowDifferentSizedItemsProperty =
        AvaloniaProperty.Register<VirtualizingWrapPanel, bool>(nameof(AllowDifferentSizedItems));

    /// <summary>
    ///     Defines the <see cref="ItemSizeProvider" /> property.
    /// </summary>
    public static readonly StyledProperty<IItemSizeProvider?> ItemSizeProviderProperty =
        AvaloniaProperty.Register<VirtualizingWrapPanel, IItemSizeProvider?>(nameof(ItemSizeProvider));

    /// <summary>
    ///     Defines the <see cref="SpacingMode" /> property.
    /// </summary>
    public static readonly StyledProperty<SpacingMode> SpacingModeProperty =
        AvaloniaProperty.Register<VirtualizingWrapPanel, SpacingMode>(nameof(SpacingMode), SpacingMode.Uniform);

    /// <summary>
    ///     Defines the <see cref="StretchItems" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> StretchItemsProperty =
        AvaloniaProperty.Register<VirtualizingWrapPanel, bool>(nameof(StretchItems));

    /// <summary>
    ///     Defines the <see cref="IsGridLayoutEnabled" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsGridLayoutEnabledProperty =
        AvaloniaProperty.Register<VirtualizingWrapPanel, bool>(nameof(IsGridLayoutEnabled), true);


    /// <summary>
    ///     Defines the <see cref="CacheRows" /> property.
    /// </summary>
    public static readonly StyledProperty<int> CacheRowsProperty =
        AvaloniaProperty.Register<VirtualizingWrapPanel, int>(nameof(CacheRows), 2);

    /// <summary>
    ///     Defines the RecycleKey attached property.
    /// </summary>
    private static readonly AttachedProperty<object?> recycleKeyProperty =
        AvaloniaProperty.RegisterAttached<VirtualizingWrapPanel, Control, object?>("RecycleKey");

    private static readonly object sItemIsItsOwnContainer = new();

    /// <summary>
    ///     Defines the <see cref="AreHorizontalSnapPointsRegular" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> AreHorizontalSnapPointsRegularProperty =
        StackPanel.AreHorizontalSnapPointsRegularProperty.AddOwner<VirtualizingWrapPanel>();

    /// <summary>
    ///     Defines the <see cref="AreVerticalSnapPointsRegular" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> AreVerticalSnapPointsRegularProperty =
        StackPanel.AreVerticalSnapPointsRegularProperty.AddOwner<VirtualizingWrapPanel>();

    /// <summary>
    ///     Defines the <see cref="HorizontalSnapPointsChanged" /> event.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> HorizontalSnapPointsChangedEvent =
        RoutedEvent.Register<VirtualizingWrapPanel, RoutedEventArgs>(
            nameof(HorizontalSnapPointsChanged),
            RoutingStrategies.Bubble);

    /// <summary>
    ///     Defines the <see cref="VerticalSnapPointsChanged" /> event.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> VerticalSnapPointsChangedEvent =
        RoutedEvent.Register<VirtualizingWrapPanel, RoutedEventArgs>(
            nameof(VerticalSnapPointsChanged),
            RoutingStrategies.Bubble);

    private readonly List<Size> childSizesReuse = new();
    private readonly List<(int index, double y)> previousRowsReuse = new();
    private readonly Action<Control, int> recycleElement;
    private readonly Action<Control> recycleElementOnItemRemoved;

    private readonly List<RowInfo> rowCache = new();

    private readonly List<Control> rowChildrenReuse = new();
    private readonly Action<Control, int, int> updateElementIndex;
    private Size? averageItemSizeCache;
    private int endItemIndex = -1;
    private Control? focusedElement;
    private int focusedIndex = -1;
    private bool isInLayout;
    private bool isWaitingForViewportUpdate;
    private double lastLayoutWidth;
    private RealizedWrapElements? measureElements;
    private double? navigationAnchor;
    private RealizedWrapElements? realizedElements;
    private Dictionary<object, Queue<Control>>? recyclePool;
    private IScrollAnchorProvider? scrollAnchorProvider;
    private Control? scrollToElement;
    private int scrollToIndex = -1;
    private Size? sizeOfFirstItem;

    private int startItemIndex = -1;

    private double startItemOffsetX;
    private double startItemOffsetY;
    private Rect viewport;

    static VirtualizingWrapPanel()
    {
        AffectsMeasure<VirtualizingWrapPanel>(
            OrientationProperty,
            ItemSizeProperty,
            AllowDifferentSizedItemsProperty,
            ItemSizeProviderProperty);

        AffectsArrange<VirtualizingWrapPanel>(
            SpacingModeProperty,
            StretchItemsProperty,
            IsGridLayoutEnabledProperty);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="VirtualizingWrapPanel" /> class.
    /// </summary>
    public VirtualizingWrapPanel()
    {
        recycleElement = RecycleElement;
        recycleElementOnItemRemoved = RecycleElementOnItemRemoved;
        updateElementIndex = UpdateElementIndex;
        EffectiveViewportChanged += OnEffectiveViewportChanged;
    }

    protected int LastNavigationIndex { get; private set; } = -1;

    /// <summary>
    ///     Gets or sets a value that specifies the orientation in which items are arranged before wrapping.
    ///     The default value is <see cref="Orientation.Horizontal" />.
    /// </summary>
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value that specifies the size of the items. The default value is <see cref="emptySize" />.
    ///     If the value is <see cref="emptySize" /> the item size is determined by measuring the first realized item.
    /// </summary>
    public Size ItemSize
    {
        get => GetValue(ItemSizeProperty);
        set => SetValue(ItemSizeProperty, value);
    }

    /// <summary>
    ///     Specifies whether items can have different sizes. The default value is false. If this property is enabled,
    ///     it is strongly recommended to also set the <see cref="ItemSizeProvider" /> property. Otherwise, the position
    ///     of the items is not always guaranteed to be correct.
    /// </summary>
    public bool AllowDifferentSizedItems
    {
        get => GetValue(AllowDifferentSizedItemsProperty);
        set => SetValue(AllowDifferentSizedItemsProperty, value);
    }

    /// <summary>
    ///     Specifies an instance of <see cref="IItemSizeProvider" /> which provides the size of the items. In order to allow
    ///     different sized items, also enable the <see cref="AllowDifferentSizedItems" /> property.
    /// </summary>
    public IItemSizeProvider? ItemSizeProvider
    {
        get => GetValue(ItemSizeProviderProperty);
        set => SetValue(ItemSizeProviderProperty, value);
    }

    /// <summary>
    ///     Gets or sets the spacing mode used when arranging the items. The default value is
    ///     <see cref="SpacingMode.Uniform" />.
    /// </summary>
    public SpacingMode SpacingMode
    {
        get => GetValue(SpacingModeProperty);
        set => SetValue(SpacingModeProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value that specifies if the items get stretched to fill up remaining space. The default value is
    ///     false.
    /// </summary>
    /// <remarks>
    ///     The MaxWidth and MaxHeight properties of the ItemContainerStyle can be used to limit the stretching.
    ///     In this case the use of the remaining space will be determined by the SpacingMode property.
    /// </remarks>
    public bool StretchItems
    {
        get => GetValue(StretchItemsProperty);
        set => SetValue(StretchItemsProperty, value);
    }

    /// <summary>
    ///     Specifies whether the items are arranged in a grid-like layout. The default value is <c>true</c>.
    ///     When set to <c>true</c>, the items are arranged based on the number of items that can fit in a row.
    ///     When set to <c>false</c>, the items are arranged based on the number of items that are actually placed in the row.
    /// </summary>
    /// <remarks>
    ///     If <see cref="AllowDifferentSizedItems" /> is enabled, this property has no effect and the items are always
    ///     arranged based on the number of items that are actually placed in the row.
    /// </remarks>
    public bool IsGridLayoutEnabled
    {
        get => GetValue(IsGridLayoutEnabledProperty);
        set => SetValue(IsGridLayoutEnabledProperty, value);
    }

    /// <summary>
    ///     Number of rows to keep cached above and below the visible viewport.
    ///     Increasing this can reduce layout recalculations while scrolling at the cost of extra realized work.
    /// </summary>
    public int CacheRows
    {
        get => GetValue(CacheRowsProperty);
        set => SetValue(CacheRowsProperty, value);
    }

    /// <summary>
    ///     Gets the index of the first realized element, or -1 if no elements are realized.
    /// </summary>
    public int FirstRealizedIndex => realizedElements?.FirstIndex ?? -1;

    /// <summary>
    ///     Gets the index of the last realized element, or -1 if no elements are realized.
    /// </summary>
    public int LastRealizedIndex => realizedElements?.LastIndex ?? -1;

    /// <inheritdoc />
    public Size GetSizeForItem(object item)
    {
        return GetUpfrontKnownItemSize(item) ?? GetAssumedItemSize(item);
    }

    /// <inheritdoc />
    public IReadOnlyList<double> GetIrregularSnapPoints(Orientation orientation,
        SnapPointsAlignment snapPointsAlignment)
    {
        if (realizedElements is null) return Array.Empty<double>();

        return new VirtualizingWrapPanelSnapPointList(
            realizedElements,
            Items.Count,
            orientation,
            Orientation,
            snapPointsAlignment,
            GetWidth(GetAverageItemSize()));
    }

    /// <inheritdoc />
    public double GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment,
        out double offset)
    {
        offset = 0f;
        var firstChild = GetFirstRealizedContainer();

        if (firstChild == null) return 0;

        double snapPoint = 0;

        switch (orientation)
        {
            case Orientation.Horizontal:
                if (!AreHorizontalSnapPointsRegular)
                    throw new InvalidOperationException();

                snapPoint = firstChild.Bounds.Width;
                switch (snapPointsAlignment)
                {
                    case SnapPointsAlignment.Near:
                        offset = firstChild.Bounds.Left;
                        break;
                    case SnapPointsAlignment.Center:
                        offset = firstChild.Bounds.Center.X;
                        break;
                    case SnapPointsAlignment.Far:
                        offset = firstChild.Bounds.Right;
                        break;
                }

                break;
            case Orientation.Vertical:
                if (!AreVerticalSnapPointsRegular)
                    throw new InvalidOperationException();
                snapPoint = firstChild.Bounds.Height;
                switch (snapPointsAlignment)
                {
                    case SnapPointsAlignment.Near:
                        offset = firstChild.Bounds.Top;
                        break;
                    case SnapPointsAlignment.Center:
                        offset = firstChild.Bounds.Center.Y;
                        break;
                    case SnapPointsAlignment.Far:
                        offset = firstChild.Bounds.Bottom;
                        break;
                }

                break;
        }

        return snapPoint;
    }

    /// <summary>
    ///     Occurs when the measurements for horizontal snap points change.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? HorizontalSnapPointsChanged
    {
        add => AddHandler(HorizontalSnapPointsChangedEvent, value);
        remove => RemoveHandler(HorizontalSnapPointsChangedEvent, value);
    }

    /// <summary>
    ///     Occurs when the measurements for vertical snap points change.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? VerticalSnapPointsChanged
    {
        add => AddHandler(VerticalSnapPointsChangedEvent, value);
        remove => RemoveHandler(VerticalSnapPointsChangedEvent, value);
    }

    /// <summary>
    ///     Gets or sets whether the horizontal snap points for the <see cref="StackPanel" /> are equidistant from each other.
    /// </summary>
    public bool AreHorizontalSnapPointsRegular
    {
        get => GetValue(AreHorizontalSnapPointsRegularProperty);
        set => SetValue(AreHorizontalSnapPointsRegularProperty, value);
    }

    /// <summary>
    ///     Gets or sets whether the vertical snap points for the <see cref="StackPanel" /> are equidistant from each other.
    /// </summary>
    public bool AreVerticalSnapPointsRegular
    {
        get => GetValue(AreVerticalSnapPointsRegularProperty);
        set => SetValue(AreVerticalSnapPointsRegularProperty, value);
    }

    private void ClearRowCache()
    {
        rowCache.Clear();
    }

    private void AddRowCacheEntry(int startIndex, double y, double height, int count, double summedUpChildWidth)
    {
        if (count <= 0)
            return;

        int index = -1;
        // Optimization: check last entry first as it's the most common case during forward realization
        if (rowCache.Count > 0)
        {
            var last = rowCache[^1];
            if (last.StartIndex == startIndex)
            {
                index = rowCache.Count - 1;
            }
            else if (last.StartIndex < startIndex)
            {
                // New entry after the last one, will be handled by the Add at the end
            }
            else
            {
                // Out of order or scrolling up, find insertion point or existing entry using binary search
                int lo = 0, hi = rowCache.Count - 1;
                while (lo <= hi)
                {
                    int mid = lo + hi >> 1;
                    if (rowCache[mid].StartIndex == startIndex)
                    {
                        index = mid;
                        break;
                    }

                    if (rowCache[mid].StartIndex < startIndex)
                        lo = mid + 1;
                    else
                        hi = mid - 1;
                }

                if (index < 0)
                {
                    // Insertion point is at 'lo'
                    index = lo;
                    rowCache.Insert(index, new RowInfo { StartIndex = startIndex, Y = y, Height = height, Count = count, SummedUpChildWidth = summedUpChildWidth });
                    if (index + 1 < rowCache.Count) rowCache.RemoveRange(index + 1, rowCache.Count - (index + 1));
                    goto Trim;
                }
            }
        }

        if (index >= 0)
        {
            var existing = rowCache[index];
            // If the row info changed, we must invalidate all subsequent rows in the cache
            // as their Y position depends on this row.
            if (!existing.Y.IsCloseTo(y) || !existing.Height.IsCloseTo(height) || existing.Count != count || !existing.SummedUpChildWidth.IsCloseTo(summedUpChildWidth))
            {
                if (index + 1 < rowCache.Count) rowCache.RemoveRange(index + 1, rowCache.Count - (index + 1));
                existing.Y = y;
                existing.Height = height;
                existing.Count = count;
                existing.SummedUpChildWidth = summedUpChildWidth;
            }
        }
        else
        {
            rowCache.Add(new RowInfo { StartIndex = startIndex, Y = y, Height = height, Count = count, SummedUpChildWidth = summedUpChildWidth });
            index = rowCache.Count - 1;
        }

        Trim:
        if (rowCache.Count > row_cache_capacity)
        {
            // If we are adding/updating near the start, trim from the end.
            // Otherwise trim from the start.
            if (index < rowCache.Count / 2)
                rowCache.RemoveAt(rowCache.Count - 1);
            else
                rowCache.RemoveAt(0);
        }
    }


    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var items = Items;

        if (items.Count == 0)
            return default;

        var orientation = Orientation;

        double wrappingWidth = GetWidth(availableSize);
        if (double.IsInfinity(wrappingWidth)) wrappingWidth = GetWidth(viewport.Size);

        if (wrappingWidth <= 0) wrappingWidth = lastLayoutWidth;
        if (wrappingWidth <= 0) wrappingWidth = GetWidth(Bounds.Size);
        if (wrappingWidth <= 0) wrappingWidth = GetWidth(DesiredSize);
        if (wrappingWidth <= 0) wrappingWidth = fallbackItemSize.Width * 10;

        // If we're bringing an item into view, ignore any layout passes until we receive a new
        // effective viewport.
        if (isWaitingForViewportUpdate)
            return EstimateDesiredSize(orientation, items.Count, wrappingWidth);

        isInLayout = true;

        try
        {
            // _realizedElements?.ValidateStartU(Orientation);
            realizedElements ??= new RealizedWrapElements();
            measureElements ??= new RealizedWrapElements();

            // If the viewport is disjunct then we can recycle everything
            bool disjunct = startItemIndex < realizedElements.FirstIndex
                            || startItemIndex > realizedElements.LastIndex;

            if (disjunct)
                realizedElements.RecycleAllElements(recycleElement);

            // Do the measure, creating/recycling elements as necessary to fill the viewport. Don't
            // write to _realizedElements yet, only _measureElements.
            RealizeAndVirtualizeItems();

            // Now swap the measureElements and realizedElements collection.
            (measureElements, realizedElements) = (realizedElements, measureElements);
            measureElements.ResetForReuse();

            // If there is a focused element is outside the visible viewport (i.e.
            // _focusedElement is non-null), ensure it's measured.
            focusedElement?.Measure(availableSize);

            return CalculateDesiredSize(orientation, items.Count, wrappingWidth);
        }
        finally
        {
            isInLayout = false;
        }
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (realizedElements is null)
            return default;

        isInLayout = true;

        try
        {
            if (startItemIndex == -1) return finalSize;

            if (realizedElements.Count < endItemIndex - startItemIndex + 1) return finalSize;

            double x = startItemOffsetX; // + GetX(_viewport.TopLeft);
            double y = startItemOffsetY; // - GetY(_viewport.TopLeft);
            double rowHeight = 0;
            double arrangedRowHeight = 0; // max height of only realized (non-null) children
            double finalWidth = GetWidth(finalSize);
            var items = Items;

            rowChildrenReuse.Clear();
            childSizesReuse.Clear();
            double summedUpChildWidth = 0;

            for (int i = startItemIndex; i <= endItemIndex; i++)
            {
                object? item = items[i];
                var child = realizedElements.GetElement(i);

                var upfrontKnownItemSize = GetUpfrontKnownItemSize(item);

                var childSize = upfrontKnownItemSize ?? realizedElements.GetElementSize(child) ?? fallbackItemSize;

                if (rowChildrenReuse.Count > 0 && x + GetWidth(childSize) > finalWidth)
                {
                    ArrangeRow(finalWidth, rowChildrenReuse, childSizesReuse, y, summedUpChildWidth, arrangedRowHeight);
                    x = 0;
                    y += rowHeight;
                    rowHeight = 0;
                    arrangedRowHeight = 0;
                    rowChildrenReuse.Clear();
                    childSizesReuse.Clear();
                    summedUpChildWidth = 0;
                }

                x += GetWidth(childSize);
                rowHeight = Math.Max(rowHeight, GetHeight(childSize));
                if (child != null)
                {
                    rowChildrenReuse.Add(child);
                    childSizesReuse.Add(childSize);
                    summedUpChildWidth += GetWidth(childSize);
                    arrangedRowHeight = Math.Max(arrangedRowHeight, GetHeight(childSize));

                    scrollAnchorProvider?.RegisterAnchorCandidate(child);
                }
            }

            if (rowChildrenReuse.Count > 0) ArrangeRow(finalWidth, rowChildrenReuse, childSizesReuse, y, summedUpChildWidth, arrangedRowHeight);

            // Ensure that the focused element is in the correct position.
            if (focusedElement is not null && focusedIndex >= 0)
            {
                var startPoint = FindItemOffset(focusedIndex, finalWidth);

                double focusedOffsetX = GetX(startPoint);
                double focusedOffsetY = GetY(startPoint);

                var rect = Orientation == Orientation.Horizontal
                    ? new Rect(focusedOffsetX, focusedOffsetY, focusedElement.DesiredSize.Width, focusedElement.DesiredSize.Height)
                    : new Rect(focusedOffsetY, focusedOffsetX, focusedElement.DesiredSize.Width, focusedElement.DesiredSize.Height);
                focusedElement.Arrange(rect);
            }

            // Ensure that the scrollTo element is in the correct position.
            if (scrollToElement is not null && scrollToIndex >= 0)
            {
                var startPoint = FindItemOffset(scrollToIndex, finalWidth);

                double scrollToOffsetX = GetX(startPoint);
                double scrollToOffsetY = GetY(startPoint);

                var rect = Orientation == Orientation.Horizontal
                    ? new Rect(scrollToOffsetX, scrollToOffsetY, scrollToElement.DesiredSize.Width,
                        finalSize.Height)
                    : new Rect(scrollToOffsetY, scrollToOffsetX, finalSize.Width,
                        scrollToElement.DesiredSize.Height);
                scrollToElement.Arrange(rect);
            }

            return finalSize;
        }
        finally
        {
            isInLayout = false;

            RaiseEvent(new RoutedEventArgs(Orientation == Orientation.Horizontal ? HorizontalSnapPointsChangedEvent : VerticalSnapPointsChangedEvent));
        }
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        scrollAnchorProvider = this.FindAncestorOfType<IScrollAnchorProvider>();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        scrollAnchorProvider = null;
        ClearRecyclePool();
    }

    /// <inheritdoc />
    protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
    {
        averageItemSizeCache = null;
        ClearRowCache();
        InvalidateMeasure();

        if (realizedElements is null)
            return;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                realizedElements.ItemsInserted(e.NewStartingIndex, e.NewItems!.Count, updateElementIndex);
                break;
            case NotifyCollectionChangedAction.Remove:
                realizedElements.ItemsRemoved(e.OldStartingIndex, e.OldItems!.Count, updateElementIndex,
                    recycleElementOnItemRemoved);
                break;
            case NotifyCollectionChangedAction.Replace:
                realizedElements.ItemsReplaced(e.OldStartingIndex, e.OldItems!.Count,
                    recycleElementOnItemRemoved);
                break;
            case NotifyCollectionChangedAction.Move:
                realizedElements.ItemsRemoved(e.OldStartingIndex, e.OldItems!.Count, updateElementIndex,
                    recycleElementOnItemRemoved);
                realizedElements.ItemsInserted(e.NewStartingIndex, e.NewItems!.Count, updateElementIndex);
                break;
            case NotifyCollectionChangedAction.Reset:
                realizedElements.ItemsReset(recycleElementOnItemRemoved);
                break;
        }
    }

    /// <inheritdoc />
    protected override void OnItemsControlChanged(ItemsControl? oldValue)
    {
        base.OnItemsControlChanged(oldValue);

        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= OnItemsControlPropertyChanged;
            ClearRecyclePool();
        }

        if (ItemsControl is not null)
            ItemsControl.PropertyChanged += OnItemsControlPropertyChanged;
    }

    /// <inheritdoc />
    protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
    {
        int count = Items.Count;
        var fromControl = from as Control;

        if (count == 0 || fromControl is null && direction is not NavigationDirection.First and not NavigationDirection.Last)
            return null;

        int fromIndex = fromControl != null ? IndexFromContainer(fromControl) : -1;

        if (fromIndex == -1 && direction is not NavigationDirection.First and not NavigationDirection.Last)
            return null;

        int toIndex = fromIndex;

        if (fromIndex != LastNavigationIndex) navigationAnchor = null;

        // Reset or update navigation anchor
        switch (direction)
        {
            case NavigationDirection.Up:
            case NavigationDirection.Down:
                if (Orientation == Orientation.Vertical)
                    navigationAnchor = null;
                break;
            case NavigationDirection.Left:
            case NavigationDirection.Right:
                if (Orientation == Orientation.Horizontal)
                    navigationAnchor = null;
                break;
            default:
                navigationAnchor = null;
                break;
        }

        switch (direction)
        {
            case NavigationDirection.First:
                toIndex = 0;
                break;
            case NavigationDirection.Last:
                toIndex = count - 1;
                break;
            case NavigationDirection.Next:
                NavigateRight(ref toIndex);
                break;
            case NavigationDirection.Previous:
                NavigateLeft(ref toIndex);
                break;
            case NavigationDirection.Left:
                NavigateLeft(ref toIndex);
                break;
            case NavigationDirection.Right:
                NavigateRight(ref toIndex);
                break;
            case NavigationDirection.Up:
                NavigateUp(ref toIndex);
                break;
            case NavigationDirection.Down:
                NavigateDown(ref toIndex);
                break;
            default:
                return null;
        }

        if (fromIndex == toIndex)
        {
            LastNavigationIndex = toIndex;
            return from;
        }

        if (wrap)
        {
            if (toIndex < 0)
                toIndex = count - 1;
            else if (toIndex >= count)
                toIndex = 0;
        }
        else
        {
            if (toIndex < 0)
                toIndex = 0;
            else if (toIndex >= count)
                toIndex = count - 1;
        }

        LastNavigationIndex = toIndex;
        return ScrollIntoView(toIndex);
    }

    /// <inheritdoc />
    protected override IEnumerable<Control>? GetRealizedContainers()
    {
        if (realizedElements is null) return null;

        var elements = realizedElements.Elements;
        int count = elements.Count;
        var result = new List<Control>(count);
        for (int i = 0; i < count; i++)
            if (elements[i] is { } element)
                result.Add(element);

        return result;
    }

    /// <inheritdoc />
    protected override Control? ContainerFromIndex(int index)
    {
        if (index < 0 || index >= Items.Count)
            return null;
        if (scrollToIndex == index)
            return scrollToElement;
        if (focusedIndex == index)
            return focusedElement;
        if (GetRealizedElement(index) is { } realized)
            return realized;
        if (Items[index] is Control c && ReferenceEquals(c.GetValue(recycleKeyProperty), sItemIsItsOwnContainer))
            return c;
        return null;
    }

    /// <inheritdoc />
    protected override int IndexFromContainer(Control container)
    {
        if (ReferenceEquals(container, scrollToElement))
            return scrollToIndex;
        if (ReferenceEquals(container, focusedElement))
            return focusedIndex;
        return realizedElements?.GetIndex(container) ?? -1;
    }

    private Rect GetExpectedItemRect(int index, double wrappingWidth)
    {
        var items = Items;
        if (index < 0 || index >= items.Count)
            return default;

        var start = FindItemOffset(index, wrappingWidth);
        var itemSize = GetAssumedItemSize(index, items[index]);
        double width = GetWidth(itemSize);
        double height = GetHeight(itemSize);

        if (StretchItems)
        {
            double y = GetY(start);
            int rowStartIndex = index;
            while (rowStartIndex > 0 && GetY(FindItemOffset(rowStartIndex - 1, wrappingWidth)).IsCloseTo(y))
                rowStartIndex--;

            double rowSummedUpWidth = 0;
            int rowCount = 0;
            int k = rowStartIndex;
            while (k < items.Count && GetY(FindItemOffset(k, wrappingWidth)).IsCloseTo(y))
            {
                rowSummedUpWidth += GetWidth(GetAssumedItemSize(k, items[k]));
                rowCount++;
                k++;
            }

            GetRowLayout(wrappingWidth, rowCount, rowSummedUpWidth, out double innerSpacing, out _, out double extraWidth);
            width += extraWidth;
            if (index < rowStartIndex + rowCount - 1)
                width += innerSpacing;
        }

        return CreateRect(GetX(start), GetY(start), width, height);
    }

    /// <inheritdoc />
    protected override Control? ScrollIntoView(int index)
    {
        var items = Items;

        if (isInLayout || index < 0 || index >= items.Count || realizedElements is null || !IsEffectivelyVisible)
            return null;

        double wrappingWidth = GetWrappingWidth();

        if (TopLevel.GetTopLevel(this) is not { } root)
            return null;

        var element = GetRealizedElement(index);

        if (element is not null)
        {
            var rect = GetExpectedItemRect(index, wrappingWidth);

            if (!viewport.Contains(rect))
            {
                isWaitingForViewportUpdate = true;
                InvalidateMeasure();
                root.UpdateLayout();
                isWaitingForViewportUpdate = false;
            }

            element.BringIntoView();
            return element;
        }

        var newScrollToElement = GetOrCreateElement(items, index);
        newScrollToElement.Measure(Size.Infinity);

        var expectedRect = GetExpectedItemRect(index, wrappingWidth);
        newScrollToElement.Arrange(expectedRect);

        scrollToElement = newScrollToElement;
        scrollToIndex = index;

        if (!Bounds.Contains(expectedRect) && !viewport.Contains(expectedRect))
        {
            isWaitingForViewportUpdate = true;
            root.UpdateLayout();
            isWaitingForViewportUpdate = false;
        }

        newScrollToElement.BringIntoView();
        isWaitingForViewportUpdate = !viewport.Contains(expectedRect);
        root.UpdateLayout();

        if (isWaitingForViewportUpdate)
        {
            isWaitingForViewportUpdate = false;
            InvalidateMeasure();
            root.UpdateLayout();
        }

        newScrollToElement.BringIntoView();

        var realizedElement = GetRealizedElement(index);

        if (scrollToElement is not null)
            RecycleElement(scrollToElement, scrollToIndex);

        scrollToElement = null;
        scrollToIndex = -1;
        return realizedElement;
    }

    private double GetWrappingWidth()
    {
        double width = GetWidth(viewport.Size);
        if (width <= 0) width = lastLayoutWidth;
        if (width <= 0) width = GetWidth(Bounds.Size);
        if (width <= 0) width = GetWidth(DesiredSize);
        if (width <= 0) width = fallbackItemSize.Width * 10;
        return width;
    }

    /// <summary>
    ///     Calculates the desired size of the viewport.
    /// </summary>
    /// <param name="orientation">the <see cref="Orientation" /> to use</param>
    /// <param name="itemCount">The number of items</param>
    /// <param name="wrappingWidth">the width used for wrapping</param>
    /// <returns>the desired size</returns>
    private Size CalculateDesiredSize(Orientation orientation, int itemCount, double wrappingWidth)
    {
        if (itemCount == 0) return emptySize;

        var averageItemSize = GetAverageItemSize();

        double itemWidth = GetWidth(averageItemSize);
        double itemHeight = GetHeight(averageItemSize);

        if (itemWidth == 0 || itemHeight == 0) return emptySize;

        double itemsPerRow = Math.Max(Math.Floor((wrappingWidth + epsilon) / itemWidth), 1);

        double sizeU;
        if (AllowDifferentSizedItems)
        {
            // If we have a partially populated row cache, we can use it to estimate the rest
            if (rowCache.Count > 0)
            {
                var lastRow = rowCache[^1];
                int startIndex = lastRow.StartIndex + lastRow.Count;
                int remainingItems = itemCount - startIndex;

                if (remainingItems <= 0)
                {
                    sizeU = lastRow.Y + lastRow.Height;
                }
                else
                {
                    if (ItemSizeProvider is not null)
                    {
                        double x = 0;
                        double rowHeight = 0;
                        double y = lastRow.Y + lastRow.Height;

                        for (int i = startIndex; i < itemCount; i++)
                        {
                            object? item = Items[i];
                            var itemSize = GetAssumedItemSize(i, item);

                            if (x != 0 && x + GetWidth(itemSize) > wrappingWidth)
                            {
                                x = 0;
                                y += rowHeight;
                                rowHeight = 0;
                            }

                            x += GetWidth(itemSize);
                            rowHeight = Math.Max(rowHeight, GetHeight(itemSize));
                        }

                        sizeU = y + rowHeight;
                    }
                    else
                    {
                        double remainingRows = Math.Ceiling(remainingItems / itemsPerRow);
                        sizeU = lastRow.Y + lastRow.Height + remainingRows * itemHeight;
                    }
                }
            }
            else
            {
                // No row cache and no ItemSizeProvider: use average-based estimation to avoid
                // an O(N) full-scan on every measure pass. This matches the non-different-sizes
                // path and is acceptable because GetAssumedItemSize already returns the average
                // for unrealized items in this scenario.
                sizeU = Math.Ceiling(itemCount / itemsPerRow) * itemHeight;
            }
        }
        else
        {
            sizeU = Math.Ceiling(itemCount / itemsPerRow) * itemHeight;
        }

        return orientation == Orientation.Horizontal ? new Size(wrappingWidth, sizeU) : new Size(sizeU, wrappingWidth);
    }

    /// <summary>
    ///     Estimates the desired size
    /// </summary>
    /// <param name="orientation">the <see cref="Orientation" /> to use</param>
    /// <param name="itemCount">The number of items</param>
    /// <param name="wrappingWidth">the width used for wrapping</param>
    /// <returns>the estimated desired size</returns>
    private Size EstimateDesiredSize(Orientation orientation, int itemCount, double wrappingWidth)
    {
        if (scrollToIndex >= 0 && scrollToElement is not null)
        {
            // We have an element to scroll to, so we can estimate the desired size based on the
            // element's position and the remaining elements.
            int remainingItems = itemCount - scrollToIndex - 1;

            if (remainingItems <= 0)
            {
                double u = GetY(scrollToElement.Bounds.BottomRight);
                return orientation == Orientation.Horizontal ? new Size(wrappingWidth, u) : new Size(u, wrappingWidth);
            }

            double sizeU;

            if (AllowDifferentSizedItems && ItemSizeProvider is not null)
            {
                double x;
                double rowHeight = 0;

                // Find start of row for _scrollToIndex
                var start = FindItemOffset(scrollToIndex, wrappingWidth);
                x = GetX(start);
                double y = GetY(start);

                for (int i = scrollToIndex; i < itemCount; i++)
                {
                    var itemSize = i == scrollToIndex ? new Size(GetWidth(scrollToElement.Bounds.Size), GetHeight(scrollToElement.Bounds.Size)) : GetAssumedItemSize(i, Items[i]);

                    if (x != 0 && x + GetWidth(itemSize) > wrappingWidth)
                    {
                        x = 0;
                        y += rowHeight;
                        rowHeight = 0;
                    }

                    x += GetWidth(itemSize);
                    rowHeight = Math.Max(rowHeight, GetHeight(itemSize));
                }

                sizeU = y + rowHeight;
            }
            else
            {
                var avgSize = GetAverageItemSize();
                double avgWidth = GetWidth(avgSize);
                double itemsPerRow = Math.Max(Math.Floor((wrappingWidth + epsilon) / avgWidth), 1);
                int remainingRows = (int)Math.Ceiling(remainingItems / itemsPerRow);
                double u = GetY(scrollToElement.Bounds.BottomRight);
                sizeU = u + remainingRows * GetHeight(avgSize);
            }

            return orientation == Orientation.Horizontal ? new Size(wrappingWidth, sizeU) : new Size(sizeU, wrappingWidth);
        }

        return DesiredSize;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == OrientationProperty)
        {
            ClearRowCache();
            InvalidateMeasure();
            InvalidateArrange();
            // Defer ScrollIntoView until after the layout triggered above has completed.
            // Calling it synchronously here risks reentrancy because OnPropertyChanged
            // fires before Measure/Arrange.
            Dispatcher.UIThread.Post(() => ScrollIntoView(0), DispatcherPriority.Background);
        }

        if (change.Property == AllowDifferentSizedItemsProperty
            || change.Property == ItemSizeProperty
            || change.Property == IsGridLayoutEnabledProperty
            || change.Property == StretchItemsProperty
            || change.Property == CacheRowsProperty)
        {
            foreach (var child in Children) child.InvalidateMeasure();

            ClearRowCache();
            InvalidateMeasure();
            InvalidateArrange();
        }


        base.OnPropertyChanged(change);
    }

    /// <summary>
    ///     Realizes visible items and virtualizes non-visible items
    /// </summary>
    private void RealizeAndVirtualizeItems()
    {
        FindStartIndexAndOffset();
        VirtualizeItemsBeforeStartIndex();
        RealizeItemsAndFindEndIndex();
        VirtualizeItemsAfterEndIndex();
    }

    /// <summary>
    ///     Calculates the predicted average item size
    /// </summary>
    /// <returns>the estimated average Size</returns>
    private Size GetAverageItemSize()
    {
        if (!ItemSize.NearlyEquals(emptySize)) return ItemSize;

        if (!AllowDifferentSizedItems) return sizeOfFirstItem ?? fallbackItemSize;

        return averageItemSizeCache ??= CalculateAverageItemSize();
    }

    /// <summary>
    ///     Calculates the start offset for a given item index
    /// </summary>
    /// <param name="itemIndex">the index of the requested item</param>
    /// <param name="wrappingWidth">the width used for wrapping</param>
    /// <returns>the starting point</returns>
    private Point FindItemOffset(int itemIndex, double? wrappingWidth = null)
    {
        double x, y = 0, rowHeight = 0;

        if (!AllowDifferentSizedItems && Items.Count > 0)
        {
            double itemWidth = GetWidth(GetAssumedItemSize(Items[0]));
            double itemHeight = GetHeight(GetAssumedItemSize(Items[0]));

            if (itemWidth == 0 || itemHeight == 0) return new Point();

            double viewportWidth = wrappingWidth ?? GetWidth(viewport.Size);
            if (viewportWidth <= 0) viewportWidth = lastLayoutWidth;
            if (viewportWidth <= 0) viewportWidth = GetWidth(Bounds.Size);
            if (viewportWidth <= 0) viewportWidth = GetWidth(DesiredSize);
            if (viewportWidth <= 0) viewportWidth = fallbackItemSize.Width * 10; // Extreme fallback

            int itemsPerRow = (int)Math.Max(Math.Floor((viewportWidth + epsilon) / itemWidth), 1);

            int itemRowIndex = (int)Math.Floor(itemIndex * 1.0 / itemsPerRow);
            y = itemRowIndex * itemHeight;

            GetRowLayout(viewportWidth, itemsPerRow, itemsPerRow * itemWidth, out double innerSpacing, out double outerSpacing, out double extraWidth);
            int indexInRow = itemIndex - itemRowIndex * itemsPerRow;
            x = outerSpacing + indexInRow * (itemWidth + extraWidth + innerSpacing);

            return CreatePoint(x, y);
        }

        double effectiveWrappingWidth = wrappingWidth ?? GetWidth(viewport.Size);
        if (effectiveWrappingWidth <= 0) effectiveWrappingWidth = lastLayoutWidth;
        if (effectiveWrappingWidth <= 0) effectiveWrappingWidth = GetWidth(Bounds.Size);
        if (effectiveWrappingWidth <= 0) effectiveWrappingWidth = GetWidth(DesiredSize);
        if (effectiveWrappingWidth <= 0) effectiveWrappingWidth = fallbackItemSize.Width * 10; // Extreme fallback

        int startIndex = 0;

        // Try to use row cache to quickly jump to the correct row and then accumulate within the row
        if (rowCache.Count > 0)
        {
            int lo = 0, hi = rowCache.Count - 1, best = -1;
            while (lo <= hi)
            {
                int mid = lo + hi >> 1;
                var r = rowCache[mid];
                if (r.StartIndex <= itemIndex)
                {
                    best = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            if (best >= 0)
            {
                var row = rowCache[best];
                y = row.Y;
                rowHeight = row.Height;

                // If the item is within this row, we can calculate its X using row info
                if (itemIndex < row.StartIndex + row.Count)
                {
                    GetRowLayout(effectiveWrappingWidth, row.Count, row.SummedUpChildWidth, out double innerSpacing, out double outerSpacing, out double extraWidth);
                    x = outerSpacing;
                    for (int i = row.StartIndex; i < itemIndex; i++)
                    {
                        var size = GetAssumedItemSize(i, Items[i]);
                        x += GetWidth(size) + extraWidth + innerSpacing;
                    }

                    return CreatePoint(x, y);
                }

                // Item is beyond this row.
                // If it's the last row in cache, it might be partial, so we start linear scan from its start.
                if (best == rowCache.Count - 1)
                {
                    startIndex = row.StartIndex;
                    y = row.Y;
                }
                else
                {
                    y += rowHeight;
                    startIndex = row.StartIndex + row.Count;
                }

                rowHeight = 0;
            }
        }

        // Fallback or continuation: linear accumulation
        {
            int currentRowStartIndex = startIndex;
            int currentRowCount = 0;
            double currentRowSummedUpWidth = 0;
            x = 0;

            for (int i = startIndex; i < Items.Count; i++)
            {
                var itemSize = GetAssumedItemSize(i, Items[i]);
                double itemWidth = GetWidth(itemSize);

                if (currentRowCount > 0 && x + itemWidth > effectiveWrappingWidth + epsilon)
                {
                    // Current row is finished. Check if target was in it.
                    if (itemIndex < i)
                    {
                        // Target was in the row we just finished.
                        GetRowLayout(effectiveWrappingWidth, currentRowCount, currentRowSummedUpWidth, out double innerSpacing, out double outerSpacing, out double extraWidth);
                        double finalX = outerSpacing;
                        for (int j = currentRowStartIndex; j < itemIndex; j++)
                        {
                            var s = GetAssumedItemSize(j, Items[j]);
                            finalX += GetWidth(s) + extraWidth + innerSpacing;
                        }

                        return CreatePoint(finalX, y);
                    }

                    x = 0;
                    y += rowHeight;
                    rowHeight = 0;
                    currentRowSummedUpWidth = 0;
                    currentRowStartIndex = i;
                    currentRowCount = 0;
                }

                x += itemWidth;
                currentRowSummedUpWidth += itemWidth;
                rowHeight = Math.Max(rowHeight, GetHeight(itemSize));
                currentRowCount++;

                if (i == itemIndex && i == Items.Count - 1)
                {
                    // It's the last item and it's our target.
                    GetRowLayout(effectiveWrappingWidth, currentRowCount, currentRowSummedUpWidth, out double innerSpacing, out double outerSpacing, out double extraWidth);
                    double finalX = outerSpacing;
                    for (int j = currentRowStartIndex; j < itemIndex; j++)
                    {
                        var s = GetAssumedItemSize(j, Items[j]);
                        finalX += GetWidth(s) + extraWidth + innerSpacing;
                    }

                    return CreatePoint(finalX, y);
                }
            }

            // Check if it's in the last (possibly unfinished) row
            if (itemIndex >= currentRowStartIndex && itemIndex < currentRowStartIndex + currentRowCount)
            {
                GetRowLayout(effectiveWrappingWidth, currentRowCount, currentRowSummedUpWidth, out double innerSpacing, out double outerSpacing, out double extraWidth);
                double finalX = outerSpacing;
                for (int j = currentRowStartIndex; j < itemIndex; j++)
                {
                    var s = GetAssumedItemSize(j, Items[j]);
                    finalX += GetWidth(s) + extraWidth + innerSpacing;
                }

                return CreatePoint(finalX, y);
            }

            return CreatePoint(0, y);
        }
    }

    /// <summary>
    ///     Calculates the anchor index and scroll offset for the anchor
    /// </summary>
    private void FindStartIndexAndOffset()
    {
        double startOffsetY = DetermineStartOffsetY();

        if (startOffsetY <= 0)
        {
            startItemIndex = Items.Count > 0 ? 0 : -1;
            startItemOffsetX = 0;
            startItemOffsetY = 0;
            return;
        }

        startItemIndex = -1;

        double x = 0, y = 0, rowHeight = 0;
        int indexOfFirstRowItem = 0;

        int itemIndex = 0;
        double wrappingWidth = GetWrappingWidth();

        // Use cached rows if available to quickly resolve the starting row
        if (rowCache.Count > 0)
        {
            int lo = 0, hi = rowCache.Count - 1, best = -1;
            while (lo <= hi)
            {
                int mid = lo + hi >> 1;
                var r = rowCache[mid];
                if (r.Y <= startOffsetY)
                {
                    best = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            if (best >= 0)
            {
                // Found a row that starts at or before startOffsetY
                var foundRow = rowCache[best];

                if (startOffsetY < foundRow.Y + foundRow.Height)
                {
                    // This row (or one before it) contains startOffsetY
                    int targetIndex = Math.Max(0, best - Math.Max(0, CacheRows));
                    var r = rowCache[targetIndex];
                    startItemIndex = r.StartIndex;
                    startItemOffsetX = 0;
                    startItemOffsetY = r.Y;
                    return;
                }

                // startOffsetY is beyond the foundRow, use it as a starting point for linear scan
                itemIndex = foundRow.StartIndex + foundRow.Count;
                x = 0;
                y = foundRow.Y + foundRow.Height;
                rowHeight = 0;
                indexOfFirstRowItem = itemIndex;
            }
        }

        if (!AllowDifferentSizedItems && Items.Count > 0)
        {
            double itemWidth = GetWidth(GetAssumedItemSize(Items[0]));
            double itemHeight = GetHeight(GetAssumedItemSize(Items[0]));

            if (itemWidth == 0 || itemHeight == 0) return;

            double itemsPerRow = Math.Max(Math.Floor((wrappingWidth + epsilon) / itemWidth), 1);

            int startRowIndex = (int)Math.Floor(startOffsetY / itemHeight);
            startItemIndex = (int)(startRowIndex * itemsPerRow);
            startItemOffsetX = 0;
            startItemOffsetY = startRowIndex * itemHeight;

            // Apply CacheRows
            int rowsToMoveUp = Math.Max(0, CacheRows);
            int actualRowsToMoveUp = Math.Min(startRowIndex, rowsToMoveUp);
            startItemIndex -= (int)(actualRowsToMoveUp * itemsPerRow);
            startItemOffsetY -= actualRowsToMoveUp * itemHeight;

            return;
        }

        if (AllowDifferentSizedItems && Items.Count > 0)
        {
            previousRowsReuse.Clear();

            // Linear scan fallback
            for (; itemIndex < Items.Count; itemIndex++)
            {
                object? item = Items[itemIndex];
                var itemSize = GetAssumedItemSize(itemIndex, item);

                if (x + GetWidth(itemSize) > wrappingWidth && x != 0)
                {
                    previousRowsReuse.Add((indexOfFirstRowItem, y));
                    if (previousRowsReuse.Count > CacheRows + 1)
                        previousRowsReuse.RemoveAt(0);

                    x = 0;
                    y += rowHeight;
                    rowHeight = 0;
                    indexOfFirstRowItem = itemIndex;
                }

                x += GetWidth(itemSize);
                rowHeight = Math.Max(rowHeight, GetHeight(itemSize));

                if (y + rowHeight > startOffsetY)
                {
                    // Found the row containing startOffsetY.
                    // Move back by CacheRows if possible.
                    if (previousRowsReuse.Count > 0)
                    {
                        var targetRow = previousRowsReuse[Math.Max(0, previousRowsReuse.Count - Math.Max(0, CacheRows))];
                        startItemIndex = targetRow.index;
                        startItemOffsetX = 0;
                        startItemOffsetY = targetRow.y;
                    }
                    else
                    {
                        startItemIndex = indexOfFirstRowItem;
                        startItemOffsetX = 0;
                        startItemOffsetY = y;
                    }

                    return;
                }
            }
        }

        // make sure that at least one item is realized to allow correct calculation of the extent
        if (startItemIndex == -1 && Items.Count > 0)
        {
            startItemIndex = Items.Count - 1;
            startItemOffsetX = x;
            startItemOffsetY = y;
        }
    }

    /// <summary>
    ///     Realizes all elements until the visible ViewPort is full
    /// </summary>
    private void RealizeItemsAndFindEndIndex()
    {
        if (startItemIndex == -1)
        {
            endItemIndex = -1;
            return;
        }

        int newEndItemIndex = Items.Count - 1;
        bool endItemIndexFound = false;

        double endOffsetY = DetermineEndOffsetY();

        double wrappingWidth = GetWrappingWidth();
        double x = startItemOffsetX;
        double y = startItemOffsetY;
        double rowHeight = 0;
        double currentRowSummedUpWidth = 0;
        int currentRowStartIndex = startItemIndex;
        int currentRowCount = 0;
        bool endRowReached = false;
        int extraRowsToRealize = Math.Max(0, CacheRows);

        for (int itemIndex = startItemIndex; itemIndex <= newEndItemIndex; itemIndex++)
        {
            if (itemIndex == 0) sizeOfFirstItem = null;

            object? item = Items[itemIndex];

            var container = GetOrCreateElement(Items, itemIndex);

            if (container == scrollToElement)
            {
                scrollToIndex = -1;
                scrollToElement = null;
            }

            var upfrontKnownItemSize = GetUpfrontKnownItemSize(item);

            // Prefer measuring with a concrete size when truly known (ItemSize, _sizeOfFirstItem, or provider).
            // If unknown, use Size.Infinity so the template can produce its natural DesiredSize.
            var measureSize = upfrontKnownItemSize
                              ?? sizeOfFirstItem
                              ?? (!ItemSize.NearlyEquals(emptySize) ? ItemSize : null);

            // Optimization: Skip Measure if the container already has the correct desired size.
            // However, we MUST measure if the container was just recycled (e.g. from GetOrCreateElement)
            // because it might have a different item now.
            // Avalonia's VirtualizingPanel usually handles this, but since we are doing custom realization:
            container.Measure(measureSize ?? Size.Infinity);

            var containerSize = DetermineContainerSize(item, container, upfrontKnownItemSize);

            if (measureElements!.GetElement(itemIndex) == null)
            {
                averageItemSizeCache = null;
                measureElements!.Add(itemIndex, container, containerSize);
            }

            if (!AllowDifferentSizedItems && sizeOfFirstItem is null) sizeOfFirstItem = containerSize;

            if (x != 0 && x + GetWidth(containerSize) > wrappingWidth + epsilon)
            {
                // finalize previous row in cache
                AddRowCacheEntry(currentRowStartIndex, y, rowHeight, currentRowCount, currentRowSummedUpWidth);

                // If we've already reached the viewport end row earlier, count down extra rows
                if (endRowReached)
                {
                    if (extraRowsToRealize <= 0)
                    {
                        newEndItemIndex = itemIndex - 1;
                        break;
                    }

                    extraRowsToRealize--;
                }

                x = 0;
                y += rowHeight;
                rowHeight = 0;
                currentRowSummedUpWidth = 0;
                currentRowStartIndex = itemIndex;
                currentRowCount = 0;
            }

            x += GetWidth(containerSize);
            currentRowSummedUpWidth += GetWidth(containerSize);
            rowHeight = Math.Max(rowHeight, GetHeight(containerSize));
            currentRowCount++;

            if (!endItemIndexFound)
                if (y >= endOffsetY
                    || !AllowDifferentSizedItems
                    && x + GetWidth(sizeOfFirstItem!.Value) > wrappingWidth
                    && y + rowHeight >= endOffsetY)
                {
                    endItemIndexFound = true;
                    endRowReached = true;
                    newEndItemIndex = itemIndex;
                }
        }

        // finalize last row
        AddRowCacheEntry(currentRowStartIndex, y, rowHeight, currentRowCount, currentRowSummedUpWidth);

        endItemIndex = newEndItemIndex;
    }

    /// <summary>
    ///     Determines the container size
    /// </summary>
    /// <param name="item">the item to use</param>
    /// <param name="container">the container</param>
    /// <param name="upfrontKnownItemSize">the known item size, if any</param>
    /// <returns></returns>
    private Size DetermineContainerSize(object? item, Control container, Size? upfrontKnownItemSize)
    {
        if (ItemSizeProvider is not null && item is not null) return ItemSizeProvider.GetSizeForItem(item);

        return upfrontKnownItemSize ?? realizedElements?.GetElementSize(container) ?? container.DesiredSize;
    }

    /// <summary>
    ///     Removes all items that are realized before start index
    /// </summary>
    private void VirtualizeItemsBeforeStartIndex()
    {
        realizedElements!.RecycleElementsBefore(startItemIndex, RecycleElement);
    }

    /// <summary>
    ///     Removes all items that are realized after start index
    /// </summary>
    private void VirtualizeItemsAfterEndIndex()
    {
        realizedElements!.RecycleElementsAfter(endItemIndex, RecycleElement);
    }

    /// <summary>
    ///     Calculates the start y-offset of the effective viewport
    /// </summary>
    /// <returns>the y-component of the effective viewport</returns>
    private double DetermineStartOffsetY()
    {
        return Math.Max(GetY(viewport.TopLeft), 0);
    }

    /// <summary>
    ///     Calculates the end y-offset of the effective viewport
    /// </summary>
    /// <returns>the y-component of the effective viewport</returns>
    private double DetermineEndOffsetY()
    {
        return Math.Max(0, GetY(viewport.BottomRight));
    }

    /// <summary>
    ///     Calculates the upfront known item size
    /// </summary>
    /// <param name="item">the item to use</param>
    /// <returns>the size of the item or null if not known</returns>
    private Size? GetUpfrontKnownItemSize(object? item)
    {
        if (!ItemSize.NearlyEquals(emptySize)) return ItemSize;

        if (item is null) return null;

        if (!AllowDifferentSizedItems && sizeOfFirstItem != null) return sizeOfFirstItem;

        if (ItemSizeProvider != null) return ItemSizeProvider.GetSizeForItem(item);

        return null;
    }

    /// <summary>
    ///     Calculates the assumed item size
    /// </summary>
    /// <param name="index">the index of the item</param>
    /// <param name="item">the item to use</param>
    /// <returns>the assumed size of the item</returns>
    private Size GetAssumedItemSize(int index, object? item)
    {
        if (GetUpfrontKnownItemSize(item) is { } upfrontKnownItemSize) return upfrontKnownItemSize;

        if (realizedElements?.GetElementSize(index) is { } cachedItemSize) return cachedItemSize;

        return GetAverageItemSize();
    }

    /// <summary>
    ///     Calculates the assumed item size
    /// </summary>
    /// <param name="item">the item to use</param>
    /// <returns>the assumed size of the item</returns>
    private Size GetAssumedItemSize(object? item)
    {
        if (GetUpfrontKnownItemSize(item) is { } upfrontKnownItemSize) return upfrontKnownItemSize;

        return GetAverageItemSize();
    }

    /// <summary>
    ///     Arranges items in a single row
    /// </summary>
    /// <param name="rowWidth">the available row width</param>
    /// <param name="children">the children to arrange</param>
    /// <param name="childSizes">the sizes of the children</param>
    /// <param name="y">the y offset of the row</param>
    /// <param name="summedUpChildWidth">the pre-calculated sum of all children's width</param>
    /// <param name="rowHeight">the pre-calculated maximum height of all realized children in the row</param>
    private void ArrangeRow(double rowWidth, List<Control> children, List<Size> childSizes, double y, double summedUpChildWidth, double rowHeight)
    {
        int childCount = children.Count;
        GetRowLayout(rowWidth, childCount, summedUpChildWidth, out double innerSpacing, out double outerSpacing, out double extraWidth);

        double x = -GetX(viewport.TopLeft) + outerSpacing;

        if (AllowDifferentSizedItems)
        {
            for (int i = 0; i < childCount; i++)
            {
                var child = children[i];
                var childSize = childSizes[i];
                child.Arrange(CreateRect(x, y, GetWidth(childSize) + extraWidth, rowHeight));
                x += GetWidth(childSize) + extraWidth + innerSpacing;
            }
        }
        else
        {
            double childWidth = GetWidth(childSizes[0]);
            double arrangedWidth = childWidth + extraWidth;
            for (int i = 0; i < childCount; i++)
            {
                var child = children[i];
                child.Arrange(CreateRect(x, y, arrangedWidth, rowHeight));
                x += arrangedWidth + innerSpacing;
            }
        }
    }

    private void GetRowLayout(double rowWidth, int actualChildCount, double summedUpChildWidth,
        out double innerSpacing, out double outerSpacing, out double extraWidthPerItem)
    {
        extraWidthPerItem = 0;
        double effectiveSummedUpWidth = summedUpChildWidth;

        if (actualChildCount > 0)
        {
            if (AllowDifferentSizedItems && StretchItems)
            {
                extraWidthPerItem = (rowWidth - summedUpChildWidth) / actualChildCount;
                effectiveSummedUpWidth = rowWidth;
            }
            else if (!AllowDifferentSizedItems)
            {
                var averageSize = GetAverageItemSize();
                double childWidth = GetWidth(averageSize);
                int itemsPerRow = IsGridLayoutEnabled ? (int)Math.Max(1, Math.Floor((rowWidth + epsilon) / childWidth)) : actualChildCount;

                if (StretchItems)
                {
                    double stretchedChildWidth = rowWidth / itemsPerRow;
                    // Note: We don't have access to children's MaxWidth here easily,
                    // but ArrangeRow handles it if needed. For estimation we use full stretch.
                    extraWidthPerItem = stretchedChildWidth - childWidth;
                    effectiveSummedUpWidth = itemsPerRow * stretchedChildWidth;
                }
                else if (IsGridLayoutEnabled)
                {
                    // Grid layout reserves a complete set of column tracks even for its final,
                    // incomplete row, so items align with the columns above.
                    effectiveSummedUpWidth = itemsPerRow * childWidth;
                }
            }
        }

        CalculateRowSpacing(rowWidth, actualChildCount, effectiveSummedUpWidth, out innerSpacing, out outerSpacing);
    }

    /// <summary>
    ///     Calculates the row spacing between the items and before and after the row
    /// </summary>
    /// <param name="rowWidth">the available row width</param>
    /// <param name="actualChildCount">the number of children in the row</param>
    /// <param name="summedUpChildWidth">the sum of all children's width</param>
    /// <param name="innerSpacing">returns the spacing between items</param>
    /// <param name="outerSpacing">returns the spacing before and after each row</param>
    private void CalculateRowSpacing(double rowWidth, int actualChildCount, double summedUpChildWidth,
        out double innerSpacing, out double outerSpacing)
    {
        int spacingChildCount = actualChildCount;

        if (!AllowDifferentSizedItems && IsGridLayoutEnabled)
        {
            var averageItemSize = GetAverageItemSize();
            double itemWidth = GetWidth(averageItemSize);
            if (itemWidth > 0)
            {
                int itemsPerRow = (int)Math.Max(1, Math.Floor((rowWidth + epsilon) / itemWidth));
                spacingChildCount = itemsPerRow;
            }
        }

        double unusedWidth = Math.Max(0, rowWidth - summedUpChildWidth);

        switch (SpacingMode)
        {
            case SpacingMode.Uniform:
                innerSpacing = outerSpacing = unusedWidth / (spacingChildCount + 1);
                break;

            case SpacingMode.BetweenItemsOnly:
                innerSpacing = unusedWidth / Math.Max(spacingChildCount - 1, 1);
                outerSpacing = 0;
                break;

            case SpacingMode.StartAndEndOnly:
                innerSpacing = 0;
                outerSpacing = unusedWidth / 2;
                break;

            case SpacingMode.None:
            default:
                innerSpacing = 0;
                outerSpacing = 0;
                break;
        }
    }

    /// <summary>
    ///     Calculates the average item size of all realized items
    /// </summary>
    /// <returns>the average item size or <see cref="fallbackItemSize" /> if no items are available</returns>
    private Size CalculateAverageItemSize()
    {
        var sizes = realizedElements?.Sizes;
        int count = sizes?.Count ?? 0;

        if (count > 0)
        {
            double totalWidth = 0;
            double totalHeight = 0;

            for (int i = 0; i < count; i++)
            {
                totalWidth += sizes![i].Width;
                totalHeight += sizes[i].Height;
            }

            return new Size(
                totalWidth / count,
                totalHeight / count);
        }

        return fallbackItemSize;
    }

    /// <summary>
    ///     This method gets called when the effective viewport got changed
    /// </summary>
    /// <param name="sender">the sender of the event</param>
    /// <param name="e">the event args</param>
    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
    {
        // var vertical = Orientation == Orientation.Vertical;
        double oldViewportStartX = GetX(viewport.TopLeft);
        double oldViewportStartY = GetY(viewport.TopLeft); // vertical ? ScrollOffset.Top : _viewport.Left;
        double oldViewportEndX = GetX(viewport.BottomRight);
        double oldViewportEndY = GetY(viewport.BottomRight); // vertical ? _viewport.Bottom : _viewport.Right;

        viewport = e.EffectiveViewport;
        isWaitingForViewportUpdate = false;

        double newViewportStartX = GetX(viewport.TopLeft);
        double newViewportStartY = GetY(viewport.TopLeft); // vertical ? _viewport.Top : _viewport.Left;
        double newViewportEndX = GetX(viewport.BottomRight);
        double newViewportEndY = GetY(viewport.BottomRight); // ? _viewport.Bottom : _viewport.Right);

        double newViewportWidth = GetWidth(viewport.Size);

        if (lastLayoutWidth.IsCloseTo(newViewportWidth))
        {
            // Optimization: Skip InvalidateMeasure if the new viewport is within what we already have realized/cached.
            // This is safe because:
            // 1. We already have the elements in _realizedElements.
            // 2. MeasureOverride would just result in the same _startItemIndex and _endItemIndex.
            // 3. We STILL call InvalidateArrange() because items might need to be repositioned relative to the viewport.

            // Compute the bottom of the last realized row from the row cache (O(1)) instead of
            // calling FindItemOffset which can be O(N) for AllowDifferentSizedItems.
            double endItemBottom = double.MaxValue;
            if (rowCache.Count > 0)
            {
                var lastRow = rowCache[^1];
                endItemBottom = lastRow.Y + lastRow.Height;
            }

            double cacheMargin = Math.Max(0, CacheRows) * GetHeight(GetAverageItemSize());
            bool withinCached = realizedElements != null
                                && startItemIndex >= 0
                                && endItemIndex >= 0
                                && newViewportStartY >= startItemOffsetY + cacheMargin
                                && newViewportEndY <= endItemBottom - cacheMargin;

            if (withinCached)
            {
                if (!oldViewportStartX.IsCloseTo(newViewportStartX)
                    || !oldViewportEndX.IsCloseTo(newViewportEndX)
                    || !oldViewportStartY.IsCloseTo(newViewportStartY)
                    || !oldViewportEndY.IsCloseTo(newViewportEndY))
                    InvalidateArrange();

                return;
            }
        }

        lastLayoutWidth = newViewportWidth;
        ClearRowCache();

        if (!oldViewportStartX.IsCloseTo(newViewportStartX)
            || !oldViewportEndX.IsCloseTo(newViewportEndX)
            || !oldViewportStartY.IsCloseTo(newViewportStartY)
            || !oldViewportEndY.IsCloseTo(newViewportEndY))
            InvalidateMeasure();
    }

    /// <summary>
    ///     This method gets called when the associated ItemsControl is changed
    /// </summary>
    /// <param name="sender">the sender of the event</param>
    /// <param name="e">the event args</param>
    private void OnItemsControlPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (focusedElement is not null && e.Property == KeyboardNavigation.TabOnceActiveElementProperty && ReferenceEquals(e.GetOldValue<IInputElement?>(), focusedElement))
        {
            // TabOnceActiveElement has moved away from _focusedElement so we can recycle it.
            RecycleElement(focusedElement, focusedIndex);
            focusedElement = null;
            focusedIndex = -1;
        }
    }

    private void NavigateLeft(ref int currentIndex)
    {
        switch (Orientation)
        {
            case Orientation.Horizontal:
                --currentIndex;
                break;

            case Orientation.Vertical:
                if (AllowDifferentSizedItems)
                {
                    currentIndex = GetIndexInRelativeRow(currentIndex, -1);
                }
                else
                {
                    int itemsPerRow =
                        (int)Math.Max(Math.Floor((GetWidth(viewport.Size) + epsilon) / GetWidth(GetAverageItemSize())), 1);
                    currentIndex -= itemsPerRow;
                }

                break;
        }
    }

    private void NavigateRight(ref int currentIndex)
    {
        switch (Orientation)
        {
            case Orientation.Horizontal:
                ++currentIndex;
                break;

            case Orientation.Vertical:
                if (AllowDifferentSizedItems)
                {
                    currentIndex = GetIndexInRelativeRow(currentIndex, 1);
                }
                else
                {
                    int itemsPerRow =
                        (int)Math.Max(Math.Floor((GetWidth(viewport.Size) + epsilon) / GetWidth(GetAverageItemSize())), 1);
                    currentIndex += itemsPerRow;
                }

                break;
        }
    }

    private void NavigateUp(ref int currentIndex)
    {
        switch (Orientation)
        {
            case Orientation.Vertical:
                --currentIndex;
                break;
            case Orientation.Horizontal:
                if (AllowDifferentSizedItems)
                {
                    currentIndex = GetIndexInRelativeRow(currentIndex, -1);
                }
                else
                {
                    int itemsPerRow =
                        (int)Math.Max(Math.Floor((GetWidth(viewport.Size) + epsilon) / GetWidth(GetAverageItemSize())), 1);
                    currentIndex -= itemsPerRow;
                }

                break;
        }
    }

    private void NavigateDown(ref int currentIndex)
    {
        switch (Orientation)
        {
            case Orientation.Vertical:
                ++currentIndex;
                break;
            case Orientation.Horizontal:
                if (AllowDifferentSizedItems)
                {
                    currentIndex = GetIndexInRelativeRow(currentIndex, 1);
                }
                else
                {
                    int itemsPerRow =
                        (int)Math.Max(Math.Floor((GetWidth(viewport.Size) + epsilon) / GetWidth(GetAverageItemSize())), 1);
                    currentIndex += itemsPerRow;
                }

                break;
        }
    }

    private int GetIndexInRelativeRow(int currentIndex, int rowOffset)
    {
        int itemCount = Items.Count;
        if (currentIndex < 0 || currentIndex >= itemCount)
            return currentIndex;

        double wrappingWidth = GetWrappingWidth();

        // --- Step 1: find source row StartIndex from cache (O(log N)) or single linear scan (O(N)) ---
        int sourceCacheIndex = -1;
        RowInfo? sourceRowHint = null;

        if (rowCache.Count > 0)
        {
            int lo = 0, hi = rowCache.Count - 1;
            while (lo <= hi)
            {
                int mid = lo + hi >> 1;
                var r = rowCache[mid];
                if (r.StartIndex <= currentIndex)
                {
                    if (currentIndex < r.StartIndex + r.Count)
                    {
                        sourceRowHint = r;
                        sourceCacheIndex = mid;
                        break;
                    }

                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }
        }

        if (sourceRowHint is null)
        {
            sourceRowHint = FindRowByLinearScan(currentIndex, wrappingWidth);
            if (sourceRowHint is null)
                return currentIndex;
        }

        // Scan source row for true Count/SummedWidth (cache entry may be incomplete if it's the last row)
        (int sourceCount, double sourceSumW) = ScanRowFromStart(sourceRowHint.StartIndex, wrappingWidth);

        // --- Step 2: compute current item X and width (no FindItemOffset) ---
        GetRowLayout(wrappingWidth, sourceCount, sourceSumW,
            out double sourceInnerSpacing, out double sourceOuterSpacing, out double sourceExtraWidth);

        double currentX = sourceOuterSpacing;
        for (int i = sourceRowHint.StartIndex; i < currentIndex; i++)
            currentX += GetWidth(GetAssumedItemSize(i, Items[i])) + sourceExtraWidth + sourceInnerSpacing;

        double rawCurrentWidth = GetWidth(GetAssumedItemSize(currentIndex, Items[currentIndex]));
        double currentWidth = rawCurrentWidth + sourceExtraWidth;
        double currentMidX = currentX + currentWidth / 2;

        // Use or initialize navigation anchor
        if (navigationAnchor.HasValue)
        {
            currentMidX = navigationAnchor.Value;
            currentWidth = 0; // Use a zero-width span when we have an anchor to avoid wide-item drift
        }
        else
        {
            navigationAnchor = currentMidX;
        }

        // --- Step 3: find target row StartIndex from cache (O(1)) or single linear scan (O(N)) ---
        int targetStartIndex;

        if (sourceCacheIndex >= 0)
        {
            int targetCacheIndex = sourceCacheIndex + rowOffset;
            if (targetCacheIndex >= 0 && targetCacheIndex < rowCache.Count)
            {
                targetStartIndex = rowCache[targetCacheIndex].StartIndex;
            }
            else
            {
                // Adjacent row is outside cache bounds
                int searchFrom = rowOffset < 0 ? sourceRowHint.StartIndex - 1 : sourceRowHint.StartIndex + sourceCount;
                if (searchFrom < 0 || searchFrom >= itemCount)
                    return currentIndex;
                var fallback = FindRowByLinearScan(searchFrom, wrappingWidth);
                if (fallback is null)
                    return currentIndex;
                targetStartIndex = fallback.StartIndex;
            }
        }
        else
        {
            int searchFrom = rowOffset < 0 ? sourceRowHint.StartIndex - 1 : sourceRowHint.StartIndex + sourceCount;
            if (searchFrom < 0 || searchFrom >= itemCount)
                return currentIndex;
            var fallback = FindRowByLinearScan(searchFrom, wrappingWidth);
            if (fallback is null)
                return currentIndex;
            targetStartIndex = fallback.StartIndex;
        }

        // --- Step 4: scan target row for true Count/SummedWidth, then find closest item ---
        (int targetCount, double targetSumW) = ScanRowFromStart(targetStartIndex, wrappingWidth);
        if (targetCount == 0)
            return currentIndex;

        GetRowLayout(wrappingWidth, targetCount, targetSumW,
            out double targetInnerSpacing, out double targetOuterSpacing, out double targetExtraWidth);

        double sourceStart = currentMidX - currentWidth / 2;
        double sourceEnd = currentMidX + currentWidth / 2;
        if (currentWidth.IsAlmostZero())
        {
            sourceStart -= epsilon;
            sourceEnd += epsilon;
        }

        int bestIndex = targetStartIndex;
        double maxOverlap = -1;
        double minDiff = double.MaxValue;
        double itemX = targetOuterSpacing;

        for (int i = 0; i < targetCount; i++)
        {
            int idx = targetStartIndex + i;
            double itemWidth = GetWidth(GetAssumedItemSize(idx, Items[idx])) + targetExtraWidth;
            double itemMidX = itemX + itemWidth / 2;

            double overlap = Math.Max(0, Math.Min(sourceEnd, itemX + itemWidth) - Math.Max(sourceStart, itemX));
            double diff = Math.Abs(itemMidX - currentMidX);

            if (overlap > maxOverlap + epsilon || Math.Abs(overlap - maxOverlap) < epsilon && diff < minDiff - epsilon)
            {
                maxOverlap = overlap;
                minDiff = diff;
                bestIndex = idx;
            }
            else if (itemX > sourceEnd && overlap.IsAlmostZero() && maxOverlap > 0)
            {
                break; // moved past the source span, done
            }

            itemX += itemWidth + targetInnerSpacing;
        }

        return bestIndex;
    }

    /// <summary>
    ///     Scans a row starting at <paramref name="startIndex" /> and accumulates items until the row wraps.
    ///     Returns the true item count and total width for that row.
    ///     This is needed because the row cache entry may record an incomplete count (the last measured row
    ///     can be cut off before all its items are processed).
    /// </summary>
    private (int Count, double SummedWidth) ScanRowFromStart(int startIndex, double wrappingWidth)
    {
        double x = 0, sumW = 0;
        int count = 0;
        for (int i = startIndex; i < Items.Count; i++)
        {
            double w = GetWidth(GetAssumedItemSize(i, Items[i]));
            if (count > 0 && x + w > wrappingWidth + epsilon)
                break;
            x += w;
            sumW += w;
            count++;
        }

        return (count, sumW);
    }

    /// <summary>
    ///     Single O(N) linear scan to find the row containing <paramref name="itemIndex" />.
    ///     Uses the row cache to find the best starting point, so in practice the scan
    ///     only covers items not yet cached.
    /// </summary>
    private RowInfo? FindRowByLinearScan(int itemIndex, double wrappingWidth)
    {
        if (itemIndex < 0 || itemIndex >= Items.Count)
            return null;

        double x = 0, y = 0, rowHeight = 0, rowSummedWidth = 0;
        int scanFrom = 0, rowStart = 0, rowCount = 0;

        // Seed from the nearest cached predecessor to avoid scanning from index 0
        if (rowCache.Count > 0)
        {
            int lo = 0, hi = rowCache.Count - 1, best = -1;
            while (lo <= hi)
            {
                int mid = lo + hi >> 1;
                if (rowCache[mid].StartIndex <= itemIndex)
                {
                    best = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            if (best >= 0)
            {
                var seed = rowCache[best];
                if (itemIndex < seed.StartIndex + seed.Count)
                    return seed; // already covered by cache
                scanFrom = seed.StartIndex + seed.Count;
                y = seed.Y + seed.Height;
                rowStart = scanFrom;
            }
        }

        for (int i = scanFrom; i < Items.Count; i++)
        {
            var size = GetAssumedItemSize(i, Items[i]);
            double w = GetWidth(size);

            if (rowCount > 0 && x + w > wrappingWidth + epsilon)
            {
                if (itemIndex >= rowStart && itemIndex < i)
                    return new RowInfo { StartIndex = rowStart, Y = y, Height = rowHeight, Count = rowCount, SummedUpChildWidth = rowSummedWidth };

                y += rowHeight;
                rowHeight = 0;
                rowSummedWidth = 0;
                rowStart = i;
                rowCount = 0;
                x = 0;
            }

            x += w;
            rowSummedWidth += w;
            rowHeight = Math.Max(rowHeight, GetHeight(size));
            rowCount++;
        }

        // Last (possibly incomplete) row
        if (itemIndex >= rowStart && itemIndex < rowStart + rowCount)
            return new RowInfo { StartIndex = rowStart, Y = y, Height = rowHeight, Count = rowCount, SummedUpChildWidth = rowSummedWidth };

        return null;
    }

    /// <summary>
    ///     Calculates a virtual X-coordinate based on the <see cref="Orientation" />
    /// </summary>
    private double GetX(Point point)
    {
        return Orientation == Orientation.Horizontal ? point.X : point.Y;
    }

    /// <summary>
    ///     Calculates a virtual Y-coordinate based on the <see cref="Orientation" />
    /// </summary>
    private double GetY(Point point)
    {
        return Orientation == Orientation.Horizontal ? point.Y : point.X;
    }

    /// <summary>
    ///     Calculates a virtual width-component based on the <see cref="Orientation" />
    /// </summary>
    private double GetWidth(Size size)
    {
        return Orientation == Orientation.Horizontal ? size.Width : size.Height;
    }

    /// <summary>
    ///     Calculates a virtual height-component based on the <see cref="Orientation" />
    /// </summary>
    private double GetHeight(Size size)
    {
        return Orientation == Orientation.Horizontal ? size.Height : size.Width;
    }

    /// <summary>
    ///     Creates a virtual Point based on the <see cref="Orientation" />
    /// </summary>
    private Point CreatePoint(double x, double y)
    {
        return Orientation == Orientation.Horizontal ? new Point(x, y) : new Point(y, x);
    }

    /// <summary>
    ///     Creates a virtual Rect based on the <see cref="Orientation" />
    /// </summary>
    private Rect CreateRect(double x, double y, double width, double height)
    {
        return Orientation == Orientation.Horizontal ? new Rect(x, y, width, height) : new Rect(y, x, height, width);
    }


    /// <summary>
    ///     Gets an existing container or creates a new one of none was present
    /// </summary>
    /// <param name="items">the available items</param>
    /// <param name="index">the index to create</param>
    /// <returns>the requested container</returns>
    private Control GetOrCreateElement(IReadOnlyList<object?> items, int index)
    {
        Debug.Assert(ItemContainerGenerator is not null);

        if (GetRealizedElement(index, ref focusedIndex, ref focusedElement) is { } focusedElement2) return focusedElement2;

        if (GetRealizedElement(index, ref scrollToIndex, ref scrollToElement) is { } scrollToElement2) return scrollToElement2;

        if (GetRealizedElement(index) is { } realized)
            return realized;

        object? item = items[index];
        var generator = ItemContainerGenerator!;

        if (generator.NeedsContainer(item, index, out object? recycleKey))
            return GetRecycledElement(item, index, recycleKey) ?? CreateElement(item, index, recycleKey);

        return GetItemAsOwnContainer(item, index);
    }

    /// <summary>
    ///     Gets the realized element or null if not available
    /// </summary>
    /// <param name="index">The container index to lookup</param>
    /// <returns>the realized container</returns>
    private Control? GetRealizedElement(int index)
    {
        return realizedElements?.GetElement(index);
    }

    /// <summary>
    ///     Gets the realized element or null if not available
    /// </summary>
    /// <param name="index">The container index to lookup</param>
    /// <param name="specialIndex">the reference to a special index, e.g. <see cref="focusedIndex" /></param>
    /// <param name="specialElement">the reference to a special element, e.g. <see cref="focusedElement" /></param>
    /// <returns>the realized container</returns>
    private static Control? GetRealizedElement(
        int index,
        ref int specialIndex,
        ref Control? specialElement)
    {
        if (specialIndex == index)
        {
            Debug.Assert(specialElement is not null);

            var result = specialElement;
            specialIndex = -1;
            specialElement = null;
            return result;
        }

        return null;
    }

    /// <summary>
    ///     Prepares a container if the item is its own container
    /// </summary>
    /// <param name="item">the item to use</param>
    /// <param name="index">the item index</param>
    /// <returns>the prepared container</returns>
    private Control GetItemAsOwnContainer(object? item, int index)
    {
        Debug.Assert(ItemContainerGenerator is not null);

        var controlItem = (Control)item!;
        var generator = ItemContainerGenerator!;

        if (!controlItem.IsSet(recycleKeyProperty))
        {
            generator.PrepareItemContainer(controlItem, controlItem, index);
            AddInternalChild(controlItem);
            controlItem.SetValue(recycleKeyProperty, sItemIsItsOwnContainer);
            generator.ItemContainerPrepared(controlItem, item, index);
        }

        controlItem.SetCurrentValue(IsVisibleProperty, true);
        return controlItem;
    }

    /// <summary>
    ///     Gets a recycled container or null if no container to recycle was available
    /// </summary>
    /// <param name="item">the item which uses the container</param>
    /// <param name="index">the item index</param>
    /// <param name="recycleKey">the recycle key</param>
    /// <returns>the recycled container</returns>
    private Control? GetRecycledElement(object? item, int index, object? recycleKey)
    {
        Debug.Assert(ItemContainerGenerator is not null);

        if (recycleKey is null)
            return null;

        var generator = ItemContainerGenerator!;

        if (recyclePool?.TryGetValue(recycleKey, out var recyclePool2) == true && recyclePool2.Count > 0)
        {
            var recycled = recyclePool2.Dequeue();
            recycled.SetCurrentValue(IsVisibleProperty, true);
            generator.PrepareItemContainer(recycled, item, index);
            generator.ItemContainerPrepared(recycled, item, index);
            return recycled;
        }

        return null;
    }

    /// <summary>
    ///     Creates a container for a given item
    /// </summary>
    /// <param name="item">the item which needs a container</param>
    /// <param name="index">the item index</param>
    /// <param name="recycleKey">the recycle key to use</param>
    /// <returns>the created element</returns>
    private Control CreateElement(object? item, int index, object? recycleKey)
    {
        Debug.Assert(ItemContainerGenerator is not null);

        var generator = ItemContainerGenerator!;
        var container = generator.CreateContainer(item, index, recycleKey);

        container.SetValue(recycleKeyProperty, recycleKey);
        generator.PrepareItemContainer(container, item, index);
        AddInternalChild(container);
        generator.ItemContainerPrepared(container, item, index);

        return container;
    }

    /// <summary>
    ///     Recycles a container
    /// </summary>
    /// <param name="element">the container to recycle</param>
    /// <param name="index">the item index</param>
    private void RecycleElement(Control element, int index)
    {
        Debug.Assert(ItemsControl is not null);
        Debug.Assert(ItemContainerGenerator is not null);

        scrollAnchorProvider?.UnregisterAnchorCandidate(element);

        object? recycleKey = element.GetValue(recycleKeyProperty);

        if (recycleKey is null)
        {
            RemoveInternalChild(element);
        }
        else if (recycleKey == sItemIsItsOwnContainer)
        {
            element.SetCurrentValue(IsVisibleProperty, false);
        }
        else if (ReferenceEquals(KeyboardNavigation.GetTabOnceActiveElement(ItemsControl), element))
        {
            focusedElement = element;
            focusedIndex = index;
        }
        else
        {
            ItemContainerGenerator!.ClearItemContainer(element);
            PushToRecyclePool(recycleKey, element);
            element.SetCurrentValue(IsVisibleProperty, false);
        }
    }

    /// <summary>
    ///     Recycles a container if the item was removed
    /// </summary>
    /// <param name="element">the container to recycle</param>
    private void RecycleElementOnItemRemoved(Control element)
    {
        Debug.Assert(ItemContainerGenerator is not null);

        scrollAnchorProvider?.UnregisterAnchorCandidate(element);

        object? recycleKey = element.GetValue(recycleKeyProperty);

        if (recycleKey is null || recycleKey == sItemIsItsOwnContainer)
        {
            RemoveInternalChild(element);
        }
        else
        {
            // RemoveInternalChild(element);
            ItemContainerGenerator!.ClearItemContainer(element);
            PushToRecyclePool(recycleKey, element);
            element.SetCurrentValue(IsVisibleProperty, false);
        }
    }

    /// <summary>
    ///     Pushes a container to the recycle pool
    /// </summary>
    /// <param name="recycleKey">the containers recycle-key</param>
    /// <param name="element">the container to recycle</param>
    private void PushToRecyclePool(object recycleKey, Control element)
    {
        recyclePool ??= new Dictionary<object, Queue<Control>>();

        if (!recyclePool.TryGetValue(recycleKey, out var pool))
        {
            pool = new Queue<Control>();
            recyclePool.Add(recycleKey, pool);
        }

        pool.Enqueue(element);

        // If the pool exceeds the cap, eject the oldest container from the visual tree.
        while (pool.Count > recycle_pool_max_size)
            RemoveInternalChild(pool.Dequeue());
    }

    private void ClearRecyclePool()
    {
        if (recyclePool is null)
            return;

        foreach (var pool in recyclePool.Values)
            while (pool.Count > 0)
                RemoveInternalChild(pool.Dequeue());

        recyclePool = null;
    }

    /// <summary>
    ///     Updates the index of an element
    /// </summary>
    /// <param name="element">the affected element</param>
    /// <param name="oldIndex">the old index</param>
    /// <param name="newIndex">the new index</param>
    private void UpdateElementIndex(Control element, int oldIndex, int newIndex)
    {
        Debug.Assert(ItemContainerGenerator is not null);

        ItemContainerGenerator.ItemContainerIndexChanged(element, oldIndex, newIndex);
    }

    private Control? GetFirstRealizedContainer()
    {
        if (realizedElements is null)
            return null;
        var elements = realizedElements.Elements;
        for (int i = 0; i < elements.Count; i++)
            if (elements[i] is { } e)
                return e;
        return null;
    }

    /// <summary>
    ///     Stores information about a row of items.
    /// </summary>
    private sealed class RowInfo
    {
        public int Count;
        public double Height;
        public int StartIndex;
        public double SummedUpChildWidth;
        public double Y;
    }
}

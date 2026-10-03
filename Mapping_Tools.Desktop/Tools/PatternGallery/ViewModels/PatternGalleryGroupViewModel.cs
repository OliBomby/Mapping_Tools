using Mapping_Tools.Desktop.Localization;

namespace Mapping_Tools.Desktop.Tools.PatternGallery.ViewModels;

/// <summary>Groups visible Pattern Gallery items by their persisted group name.</summary>
public sealed class PatternGalleryGroupViewModel : LocalizedObservableObject
{
    private readonly string groupName;
    /// <summary>Creates a group with its sorted visible items.</summary>
    /// <param name="name">The display label, with empty groups shown as None.</param>
    /// <param name="patterns">The items assigned to the group.</param>
    public PatternGalleryGroupViewModel(
        string name,
        IEnumerable<PatternGalleryItemViewModel> patterns)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(patterns);
        groupName = name;
        Patterns = patterns.ToArray();
    }

    /// <summary>Gets the display label for this group.</summary>
    public string Name => string.IsNullOrWhiteSpace(groupName) ? DesktopStrings.PatternGallery_Ungrouped : groupName;

    /// <summary>Gets the visible items in this group.</summary>
    public IReadOnlyList<PatternGalleryItemViewModel> Patterns { get; }

    /// <summary>Gets the number of visible patterns in this group.</summary>
    public int ItemCount => Patterns.Count;

    /// <summary>Gets the localized noun suffix following the separately styled visible pattern count.</summary>
    public string ItemCountText => ItemCount == 1
        ? DesktopStrings.PatternGallery_OnePattern
        : DesktopStrings.PatternGallery_ManyPatterns;
}

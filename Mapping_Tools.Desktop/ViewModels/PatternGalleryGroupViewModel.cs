using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.PatternGallery;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>Groups visible Pattern Gallery items by their persisted group name.</summary>
public sealed class PatternGalleryGroupViewModel
{
    /// <summary>Creates a group with its sorted visible items.</summary>
    /// <param name="name">The display label, with empty groups shown as None.</param>
    /// <param name="patterns">The items assigned to the group.</param>
    public PatternGalleryGroupViewModel(
        string name,
        IEnumerable<PatternGalleryItemViewModel> patterns)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(patterns);
        Name = name;
        Patterns = patterns.ToArray();
    }

    /// <summary>Gets the display label for this group.</summary>
    public string Name { get; }

    /// <summary>Gets the visible items in this group.</summary>
    public IReadOnlyList<PatternGalleryItemViewModel> Patterns { get; }

    /// <summary>Gets the number of visible patterns in this group.</summary>
    public int ItemCount => Patterns.Count;
}

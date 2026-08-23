using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.PatternGallery;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>Adapts one persisted pattern to the thumbnail gallery.</summary>
public sealed partial class PatternGalleryItemViewModel : ObservableObject
{
    private bool isSelected;

    /// <summary>Creates a gallery item for the supplied persisted pattern.</summary>
    /// <param name="pattern">The pattern metadata owned by the project.</param>
    public PatternGalleryItemViewModel(PatternGalleryPattern pattern)
    {
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
    }

    /// <summary>Gets the persisted pattern metadata.</summary>
    public PatternGalleryPattern Pattern { get; }

    /// <summary>Gets the current display name.</summary>
    public string Name => Pattern.Name;

    /// <summary>Gets or sets whether this item participates in the next export.</summary>
    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }

    /// <summary>Gets the loaded thumbnail beatmap, when it is available.</summary>
    [ObservableProperty]
    public partial Beatmap? ThumbnailBeatmap { get; private set; }

    /// <summary>Publishes a newly loaded thumbnail beatmap.</summary>
    /// <param name="beatmap">The beatmap loaded from the stored pattern, or <see langword="null" /> on failure.</param>
    internal void SetThumbnail(Beatmap? beatmap)
    {
        ThumbnailBeatmap = beatmap;
    }

    /// <summary>Refreshes bindings after persisted metadata changes.</summary>
    internal void RefreshMetadata()
    {
        OnPropertyChanged(nameof(Name));
    }

    /// <summary>Refreshes the selection binding after a bulk selection operation.</summary>
    internal void RefreshSelection()
    {
        OnPropertyChanged(nameof(IsSelected));
    }
}

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

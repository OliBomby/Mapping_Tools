using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.PatternGallery.Models;

namespace Mapping_Tools.Desktop.Tools.PatternGallery.ViewModels;

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

